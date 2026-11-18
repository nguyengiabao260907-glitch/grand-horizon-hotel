using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Infrastructure;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using LapTrinhWeb_pro.Models;

namespace LapTrinhWeb_pro.Controllers
{
    public class AccountController : Controller
    {
        private readonly HotelDbEntities db = new HotelDbEntities();
        private const int PasswordIterations = 150000;

        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, PasswordIterations))
            {
                byte[] hash = derive.GetBytes(32);
                return "PBKDF2$" + PasswordIterations + "$" +
                    Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        private static bool VerifyPassword(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return false;

            // Hỗ trợ tài khoản tạo từ phiên bản cũ (SHA256), sau khi đăng nhập sẽ nâng cấp.
            if (!stored.StartsWith("PBKDF2$", StringComparison.Ordinal))
                return FixedEquals(stored, Sha256(password));

            try
            {
                string[] parts = stored.Split('$');
                int iterations = int.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
                    return FixedEquals(Convert.ToBase64String(derive.GetBytes(expected.Length)),
                                       Convert.ToBase64String(expected));
            }
            catch { return false; }
        }

        private static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant();
        }

        private static bool FixedEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void SplitName(string fullName, out string firstName, out string lastName)
        {
            var parts = (fullName ?? "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            firstName = parts.Length > 1 ? string.Join(" ", parts.Take(parts.Length - 1)) : (parts.FirstOrDefault() ?? "");
            lastName = parts.Length > 1 ? parts[parts.Length - 1] : "";
        }

        private void SignIn(GUEST guest)
        {
            Session.Clear();
            Session["GuestId"] = guest.guest_id;
            Session["GuestName"] = (guest.first_name + " " + guest.last_name).Trim();
            Session["GuestEmail"] = guest.email;
            Session["GuestPhone"] = guest.phone;
        }

        // ===== Xác minh OTP khi đăng nhập (bản v3) =====
        // Mã OTP chỉ lưu trong Session, không lưu database/cookie/hidden field.
        private const int OtpLength = 6;
        private const int OtpExpiryMinutes = 5;
        private const int OtpMaxAttempts = 3;

        private static string GenerateOtpCode()
        {
            // RandomNumberGenerator (mã hóa an toàn), KHÔNG dùng new Random().
            byte[] bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            uint value = BitConverter.ToUInt32(bytes, 0) % 900000u;
            return (100000u + value).ToString();
        }

        private static bool IsDemoMode()
        {
            bool demo;
            return !bool.TryParse(ConfigurationManager.AppSettings["OtpDemoMode"], out demo) || demo;
        }

        private static void ClearOtpSession()
        {
            // Chỉ xóa các key OTP, không đụng tới Session đăng nhập.
            // PHẢI viết đầy đủ System.Web.HttpContext.Current: trong Controller,
            // tên HttpContext đã bị thuộc tính instance Controller.HttpContext
            // "chiếm" mất, nên hàm static mà viết HttpContext.Current sẽ báo
            // CS0120 (cần object reference) ở cả 6 dòng dưới đây.
            var session = System.Web.HttpContext.Current.Session;
            session.Remove("PendingGuestId");
            session.Remove("PendingGuestEmail");
            session.Remove("LoginOtp");
            session.Remove("LoginOtpExpiry");
            session.Remove("LoginOtpAttempts");
            session.Remove("LoginReturnUrl");
        }

        // Bắt đầu phiên OTP sau khi mật khẩu đúng: sinh mã, lưu Session, gửi mail.
        // Trả về true nếu mail đã gửi (hoặc đang ở chế độ demo — không cần gửi).
        private bool StartOtp(GUEST guest, string returnUrl)
        {
            string code = GenerateOtpCode();
            Session["PendingGuestId"] = guest.guest_id;
            Session["PendingGuestEmail"] = guest.email;
            Session["LoginOtp"] = code;
            Session["LoginOtpExpiry"] = DateTime.Now.AddMinutes(OtpExpiryMinutes);
            Session["LoginOtpAttempts"] = 0;
            Session["LoginReturnUrl"] = returnUrl;

            if (IsDemoMode()) return true; // Demo: không gửi mail, mã hiện trên trang VerifyOtp.
            bool sent = SendOtpEmail(guest.email, code);
            if (!sent)
                // Gửi mail lỗi thì vẫn cho qua trang VerifyOtp (không crash,
                // không đăng nhập luôn); mã được ghi ra Debug để demo lấy được.
                Debug.WriteLine("[OTP] Khong gui duoc mail. Ma OTP cho " + guest.email + " la: " + code);
            return sent;
        }

        private static bool SendOtpEmail(string toEmail, string code)
        {
            try
            {
                string host = ConfigurationManager.AppSettings["OtpSmtpHost"] ?? "smtp.gmail.com";
                int port;
                if (!int.TryParse(ConfigurationManager.AppSettings["OtpSmtpPort"], out port)) port = 587;
                string user = ConfigurationManager.AppSettings["OtpSmtpUser"];
                string pass = ConfigurationManager.AppSettings["OtpSmtpPass"];
                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass)) return false;

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(user, pass);
                    using (var mail = new MailMessage(user, toEmail))
                    {
                        mail.Subject = "Mã xác minh đăng nhập Grand Horizon";
                        mail.Body = "Mã xác minh đăng nhập Grand Horizon của bạn là: " + code +
                            "\nMã hết hạn sau 5 phút, không chia sẻ mã này cho ai.";
                        client.Send(mail);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OTP] Loi gui mail: " + ex.Message);
                return false;
            }
        }

        // Sau khi đăng nhập xong (đã qua OTP): về returnUrl nếu là link nội bộ,
        // nếu không thì về trang chủ. Không bao giờ redirect ra website khác.
        private ActionResult RedirectAfterLogin()
        {
            string url = Session["LoginReturnUrl"] as string;
            if (!string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url))
                return Redirect(url);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (Session["GuestId"] != null) return RedirectToAction("Index", "Home");
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, string returnUrl)
        {
            email = (email ?? "").Trim();
            ViewBag.ReturnUrl = returnUrl;
            if (email.Length == 0 || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập email và mật khẩu.";
                return View();
            }

            var guest = db.GUESTs.FirstOrDefault(g => g.email.ToLower() == email.ToLower());
            if (guest == null || !VerifyPassword(password, guest.password_hash))
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View();
            }

            // Nâng cấp SHA256 cũ sang PBKDF2 sau lần đăng nhập hợp lệ.
            if (!guest.password_hash.StartsWith("PBKDF2$", StringComparison.Ordinal))
            {
                guest.password_hash = HashPassword(password);
                db.SaveChanges();
            }

            // Bản v3: mật khẩu đúng CHƯA đăng nhập ngay — phải xác minh OTP trước.
            bool mailOk = StartOtp(guest, returnUrl);
            if (!mailOk)
                TempData["OtpMailError"] = "Không gửi được mã OTP qua email. Hãy kiểm tra lại cấu hình OtpSmtpUser/OtpSmtpPass trong Web.config (hoặc bật OtpDemoMode = true để demo). Mã OTP đã được ghi ra cửa sổ Output/Debug của Visual Studio.";
            return RedirectToAction("VerifyOtp");
        }

        // GET /Account/VerifyOtp — trang nhập mã OTP sau khi nhập mật khẩu đúng.
        [HttpGet]
        public ActionResult VerifyOtp()
        {
            if (Session["PendingGuestId"] == null) return RedirectToAction("Login");
            if (TempData["OtpInfo"] != null) ViewBag.Info = TempData["OtpInfo"];
            if (TempData["OtpMailError"] != null) ViewBag.MailError = TempData["OtpMailError"];
            ViewBag.Email = Session["PendingGuestEmail"] as string;
            // Chế độ demo: hiện mã ngay trên trang để demo trên lớp không cần mail.
            ViewBag.DemoOtp = IsDemoMode() ? (Session["LoginOtp"] as string) : null;
            return View();
        }

        // POST /Account/VerifyOtp — kiểm tra mã theo thứ tự: hết hạn -> sai mã -> đúng mã.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyOtp(string otp)
        {
            if (Session["PendingGuestId"] == null) return RedirectToAction("Login");

            string savedCode = Session["LoginOtp"] as string;
            DateTime? expiry = Session["LoginOtpExpiry"] as DateTime?;
            int attempts = Session["LoginOtpAttempts"] as int? ?? 0;
            ViewBag.Email = Session["PendingGuestEmail"] as string;
            ViewBag.DemoOtp = IsDemoMode() ? savedCode : null;

            // (a) Mã đã hết hạn (quá 5 phút) hoặc không còn mã: giữ lại
            // PendingGuestId để khách bấm "Gửi lại mã", chỉ xóa mã cũ.
            if (string.IsNullOrEmpty(savedCode) || expiry == null || DateTime.Now > expiry.Value)
            {
                Session.Remove("LoginOtp");
                Session.Remove("LoginOtpExpiry");
                Session.Remove("LoginOtpAttempts");
                ViewBag.DemoOtp = null;
                ViewBag.Error = "Mã đã hết hạn, vui lòng bấm Gửi lại mã để nhận mã mới.";
                return View();
            }

            string input = (otp ?? "").Trim();
            // (b) Sai mã: đếm số lần thử, đủ 3 lần thì hủy phiên OTP, bắt đăng nhập lại.
            if (string.IsNullOrEmpty(input) || input != savedCode)
            {
                attempts++;
                Session["LoginOtpAttempts"] = attempts;
                if (attempts >= OtpMaxAttempts)
                {
                    ClearOtpSession();
                    TempData["Error"] = "Bạn đã nhập sai mã OTP 3 lần. Vui lòng đăng nhập lại từ đầu.";
                    return RedirectToAction("Login");
                }
                ViewBag.Error = "Mã OTP không đúng, còn " + (OtpMaxAttempts - attempts) + " lần thử.";
                return View();
            }

            // (c) Đúng mã: xóa phiên OTP rồi mới đăng nhập thật.
            int pendingId = (int)Session["PendingGuestId"];
            string returnUrl = Session["LoginReturnUrl"] as string;
            ClearOtpSession();
            var guest = db.GUESTs.FirstOrDefault(g => g.guest_id == pendingId);
            if (guest == null) return RedirectToAction("Login");
            SignIn(guest);
            // SignIn() đã Clear() Session nên phải gán lại returnUrl trước khi redirect.
            Session["LoginReturnUrl"] = returnUrl;
            return RedirectAfterLogin();
        }

        // POST /Account/ResendOtp — gửi lại mã mới, reset hạn 5 phút và số lần thử.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResendOtp()
        {
            if (Session["PendingGuestId"] == null) return RedirectToAction("Login");
            int pendingId = (int)Session["PendingGuestId"];
            string returnUrl = Session["LoginReturnUrl"] as string;
            var guest = db.GUESTs.FirstOrDefault(g => g.guest_id == pendingId);
            if (guest == null)
            {
                ClearOtpSession();
                return RedirectToAction("Login");
            }
            bool mailOk = StartOtp(guest, returnUrl);
            if (!mailOk)
                TempData["OtpMailError"] = "Không gửi được mã OTP qua email. Hãy kiểm tra lại cấu hình OtpSmtpUser/OtpSmtpPass trong Web.config (hoặc bật OtpDemoMode = true để demo). Mã OTP đã được ghi ra cửa sổ Output/Debug của Visual Studio.";
            else
                TempData["OtpInfo"] = "Đã gửi lại mã mới.";
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public ActionResult Register(string returnUrl)
        {
            if (Session["GuestId"] != null) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string name, string phone, string email, string password, string confirmPassword, string returnUrl)
        {
            name = (name ?? "").Trim();
            email = (email ?? "").Trim();
            phone = (phone ?? "").Trim();
            // Bản v5: chuẩn hóa email một lần, dùng cho mọi lần so sánh bên dưới.
            string emailChuan = email.ToLowerInvariant();
            ViewBag.ReturnUrl = returnUrl;

            if (name.Length == 0 || email.Length == 0 || string.IsNullOrEmpty(password))
                ViewBag.Error = "Vui lòng nhập đầy đủ họ tên, email và mật khẩu.";
            else if (!new EmailAddressAttribute().IsValid(email))
                ViewBag.Error = "Địa chỉ email không hợp lệ.";
            else if (name.Length > 100 || email.Length > 150 || phone.Length > 20)
                ViewBag.Error = "Họ tên, email hoặc số điện thoại vượt quá độ dài cho phép.";
            else if (password.Length < 6)
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
            else if (password != confirmPassword)
                ViewBag.Error = "Mật khẩu nhập lại không khớp.";
            else
            {
                var guest = db.GUESTs.FirstOrDefault(g => g.email != null && g.email.ToLower() == emailChuan);
                if (guest != null && !string.IsNullOrEmpty(guest.password_hash))
                    ViewBag.Error = "Email này đã có tài khoản. Vui lòng đăng nhập.";
                else
                {
                    string firstName, lastName;
                    SplitName(name, out firstName, out lastName);
                    if (guest == null)
                    {
                        guest = new GUEST
                        {
                            first_name = firstName,
                            last_name = lastName,
                            email = email,
                            phone = phone,
                            password_hash = HashPassword(password),
                            membership_tier = "Standard"
                        };
                        db.GUESTs.Add(guest);
                    }
                    else
                    {
                        guest.first_name = firstName;
                        guest.last_name = lastName;
                        guest.phone = phone;
                        guest.password_hash = HashPassword(password);
                    }

                    try
                    {
                        db.SaveChanges();
                        // Đăng ký không cần OTP (theo yêu cầu bản v3), đăng nhập luôn
                        // rồi về returnUrl nếu có.
                        SignIn(guest);
                        Session["LoginReturnUrl"] = returnUrl;
                        return RedirectAfterLogin();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        // Bản v5: báo đúng lý do thật, không đổ mọi lỗi cho
                        // "email đã tồn tại". (Cột email không có UNIQUE
                        // constraint nên lỗi trùng email gần như không xảy ra;
                        // nếu thông báo lỗi có nhắc tới trùng khóa thì vẫn báo
                        // đúng là email đã có tài khoản.)
                        string reason = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                        if (dbEx.InnerException != null && dbEx.InnerException.InnerException != null)
                            reason += " | " + dbEx.InnerException.InnerException.Message;
                        if (reason.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            reason.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) >= 0)
                            ViewBag.Error = "Email này đã có tài khoản. Vui lòng đăng nhập.";
                        else
                            ViewBag.Error = "Không thể tạo tài khoản: " + reason;
                    }
                    catch (Exception ex)
                    {
                        // Lỗi ngoài SaveChanges (ví dụ mất kết nối): vẫn báo
                        // đúng lý do thật, không đổ cho email.
                        ViewBag.Error = "Không thể tạo tài khoản: " + ex.Message;
                    }
                }
            }
            return View();
        }

        [HttpGet]
        public ActionResult Logout()
        {
            ClearOtpSession();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
