const ROOMS = { standard: { name: 'Standard', price: 1200000 }, deluxe: { name: 'Deluxe', price: 2200000 }, suite: { name: 'Executive Suite', price: 4500000 }, family: { name: 'Family Suite', price: 3800000 }, ocean: { name: 'Ocean View Deluxe', price: 2800000 }, presidential: { name: 'Presidential Suite', price: 8500000 } };
const fmt = n => n.toLocaleString('vi-VN') + ' đ';
const nightsBetween = (a, b) => Math.round((new Date(b) - new Date(a)) / 86400000);

function calculateTotal() {
    const roomEl = document.getElementById('bkRoom');
    if (!roomEl) return;
    const room = ROOMS[roomEl.value];
    const inn = document.getElementById('bkIn').value, out = document.getElementById('bkOut').value;
    const nights = (inn && out) ? nightsBetween(inn, out) : 0;
    let total = 0, detail = '';
    if (nights > 0) {
        total = room.price * nights;
        detail = room.name + ' ' + fmt(room.price) + ' x ' + nights + ' đêm';
        document.querySelectorAll('.svc:checked').forEach(s => {
            const p = +s.dataset.price;
            if (s.dataset.per === 'night') { total += p * nights; detail += ' + dịch vụ ' + fmt(p) + ' x ' + nights + ' đêm'; }
            else { total += p; detail += ' + dịch vụ ' + fmt(p); }
        });
    }
    const totalEl = document.getElementById('totalAmount');
    // Mã giảm giá GRAND10 (chỉ có trên trang /Booking/Create): giảm 10% tổng tiền.
    const promoEl = document.getElementById('promoCode');
    if (promoEl && promoEl.value.trim().toUpperCase() === 'GRAND10' && total > 0) {
        const discount = total * 0.10;
        total -= discount;
        detail += ' - GRAND10 giảm 10% (' + fmt(discount) + ')';
    }
    if (totalEl) totalEl.textContent = fmt(total);
    if (totalEl) totalEl.textContent = fmt(total);
    const detailEl = document.getElementById('totalDetail');
    if (detailEl) detailEl.textContent = detail;
}

function selectRoom(key) {
    const roomEl = document.getElementById('bkRoom');
    if (!roomEl) return;
    roomEl.value = key;
    calculateTotal();
    const section = document.getElementById('dat-phong');
    if (section) section.scrollIntoView({ behavior: 'smooth' });
}

// Carousel
let cur = 0;
const slides = document.querySelectorAll('.slide');
function showSlide(i) {
    if (slides.length === 0) return;
    cur = (i + slides.length) % slides.length;
    slides.forEach((s, k) => s.classList.toggle('active', k === cur));
    const countEl = document.getElementById('slideCount');
    if (countEl) countEl.textContent = (cur + 1) + ' / ' + slides.length;
}
const prevBtn = document.getElementById('prevSlide');
if (prevBtn) prevBtn.onclick = () => showSlide(cur - 1);
const nextBtn = document.getElementById('nextSlide');
if (nextBtn) nextBtn.onclick = () => showSlide(cur + 1);
if (slides.length > 0) setInterval(() => showSlide(cur + 1), 5000);

// Ngày mặc định: hôm nay -> +2 ngày
const today = new Date(), plus2 = new Date(Date.now() + 2 * 86400000);
const iso = d => {
    const local = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 10);
};
['heroIn', 'bkIn'].forEach(id => {
    const el = document.getElementById(id);
    if (el && !el.value) el.value = iso(today);
});
['heroOut', 'bkOut'].forEach(id => {
    const el = document.getElementById(id);
    if (el && !el.value) el.value = iso(plus2);
});

const heroCheck = document.getElementById('heroCheck');
if (heroCheck) heroCheck.onclick = () => {
    const heroIn = document.getElementById('heroIn');
    const heroOut = document.getElementById('heroOut');
    const bkIn = document.getElementById('bkIn');
    const bkOut = document.getElementById('bkOut');
    if (heroIn && bkIn) bkIn.value = heroIn.value;
    if (heroOut && bkOut) bkOut.value = heroOut.value;
    calculateTotal();
    const section = document.getElementById('dat-phong');
    if (section) section.scrollIntoView({ behavior: 'smooth' });
};

// Form đặt phòng - tách thành hàm riêng để gọi lại được
// khi form là Partial View load bằng fetch
function initBookingForm() {
    const bookingForm = document.getElementById('bookingForm');
    if (!bookingForm || bookingForm.dataset.bound) return;
    bookingForm.dataset.bound = '1';

    calculateTotal();

    ['bkRoom', 'bkIn', 'bkOut'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.addEventListener('change', calculateTotal);
    });
    ['bkIn', 'bkOut'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.addEventListener('change', refreshRoomAvailability);
    });
    refreshRoomAvailability();
    document.querySelectorAll('.svc').forEach(s => s.addEventListener('change', calculateTotal));

    bookingForm.addEventListener('submit', e => {
        e.preventDefault();
        const alertBox = document.getElementById('formAlert');
        const name = document.getElementById('bkName').value.trim();
        const phone = document.getElementById('bkPhone').value.trim();
        const email = document.getElementById('bkEmail').value.trim();
        const inn = document.getElementById('bkIn').value, out = document.getElementById('bkOut').value;
        if (!name || !phone || !email) { alertBox.textContent = 'Vui lòng nhập đủ họ tên, số điện thoại và email.'; return; }
        if (!inn || !out || nightsBetween(inn, out) <= 0) { alertBox.textContent = 'Ngày trả phòng phải sau ngày nhận phòng.'; return; }
        alertBox.textContent = '';

        // Gửi lên BookingController.Create để lưu database
        const formData = new FormData(bookingForm);
        fetch(bookingForm.action || '/Booking/Create', {
            method: 'POST',
            body: new URLSearchParams(formData)
        })
            .then(r => r.json())
            .then(data => {
                // Bản v3: chưa đăng nhập thì server trả requireLogin — chuyển
                // ngay tới trang Login (giữ returnUrl), không hiện lỗi đỏ.
                if (data.requireLogin) { window.location.href = data.loginUrl || '/Account/Login'; return; }
                if (!data.success) { alertBox.textContent = data.message + (data.detail ? ' — Lỗi thật: ' + data.detail : ''); return; }
                // Trang /Booking/Create: chuyển sang trang xác nhận thay vì
                // hiện khung xác nhận inline như form ở trang chủ.
                if (bookingForm.dataset.redirect === 'confirmation') {
                    window.location.href = '/Booking/Confirmation?id=' + data.bookId;
                    return;
                }
                const room = ROOMS[document.getElementById('bkRoom').value];
                document.getElementById('confirmDetails').textContent =
                    'Cảm ơn ' + name + '! Mã đặt phòng: ' + data.bookId +
                    '. Phòng: ' + room.name + (data.room ? ' (số ' + data.room + ')' : '') +
                    ' — ' + inn + ' đến ' + out +
                    ' (' + nightsBetween(inn, out) + ' đêm). Tổng tiền: ' +
                    document.getElementById('totalAmount').textContent;
                if (data.paymentNote) document.getElementById('confirmDetails').textContent += ' ' + data.paymentNote;
                document.getElementById('bookingForm').style.display = 'none';
                document.getElementById('confirmation').style.display = 'block';
                document.getElementById('confirmation').scrollIntoView({ behavior: 'smooth' });
            })
            .catch(() => { alertBox.textContent = 'Không kết nối được server. Kiểm tra lại Controller và đường dẫn /Booking/Create.'; });
    });

    const bookAgain = document.getElementById('bookAgain');
    if (bookAgain) bookAgain.onclick = () => {
        document.getElementById('confirmation').style.display = 'none';
        document.getElementById('bookingForm').style.display = 'block';
        document.getElementById('bookingForm').reset();
        calculateTotal();
    };
}

// Bản v4: khi đổi ngày nhận/trả, hỏi server loại phòng nào còn trống
// (/Rooms/Available) và khóa các lựa chọn đã hết phòng trong dropdown.
// Nếu server không trả lời được thì giữ nguyên dropdown (không chặn đặt
// phòng) — lúc submit, server vẫn kiểm tra lại và báo "Hiện không còn phòng".
function refreshRoomAvailability() {
    const roomEl = document.getElementById('bkRoom');
    const innEl = document.getElementById('bkIn'), outEl = document.getElementById('bkOut');
    if (!roomEl || !innEl || !outEl) return;
    const inn = innEl.value, out = outEl.value;
    if (!inn || !out || nightsBetween(inn, out) <= 0) return;
    fetch('/Rooms/Available?checkin=' + encodeURIComponent(inn) + '&checkout=' + encodeURIComponent(out))
        .then(r => r.json())
        .then(data => {
            const byKey = {};
            data.forEach(t => { byKey[t.key] = t; });
            let selectedGone = false;
            Array.prototype.forEach.call(roomEl.options, opt => {
                const t = byKey[opt.value];
                const base = opt.text.replace(/ — Hết phòng$/, '');
                if (t && t.available <= 0) {
                    opt.disabled = true;
                    opt.text = base + ' — Hết phòng';
                    if (opt.selected) selectedGone = true;
                } else {
                    opt.disabled = false;
                    opt.text = base;
                }
            });
            if (selectedGone) {
                const first = Array.prototype.find.call(roomEl.options, o => !o.disabled);
                if (first) roomEl.value = first.value;
            }
            calculateTotal();
        })
        .catch(() => { /* Không chặn đặt phòng khi không hỏi được server */ });
}

initBookingForm();
