// Toggle hiện/ẩn mật khẩu cho trang đăng nhập
document.addEventListener('DOMContentLoaded', () => {
    const btn = document.querySelector('.toggle-password');
    const input = document.getElementById('passwordInput');
    if (!btn || !input) return;

    btn.addEventListener('click', () => {
        const isHidden = input.type === 'password';
        input.type = isHidden ? 'text' : 'password';
        btn.setAttribute('data-state', isHidden ? 'show' : 'hide');
        btn.setAttribute('aria-pressed', isHidden ? 'true' : 'false');
    });
});
