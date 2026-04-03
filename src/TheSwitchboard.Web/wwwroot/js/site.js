// Header scroll effect
const header = document.getElementById('site-header');
if (header) {
    window.addEventListener('scroll', () => {
        if (window.scrollY > 20) {
            header.classList.add('header-scrolled');
        } else {
            header.classList.remove('header-scrolled');
        }
    });
}

// Mobile nav toggle
const mobileMenuBtn = document.getElementById('mobile-menu-btn');
const mobileCloseBtn = document.getElementById('mobile-close-btn');
const mobileNav = document.getElementById('mobile-nav');

if (mobileMenuBtn && mobileNav && mobileCloseBtn) {
    mobileMenuBtn.addEventListener('click', () => {
        mobileNav.classList.remove('hidden');
        mobileNav.classList.add('flex');
        document.body.style.overflow = 'hidden';
    });
    mobileCloseBtn.addEventListener('click', () => {
        mobileNav.classList.add('hidden');
        mobileNav.classList.remove('flex');
        document.body.style.overflow = '';
    });
    mobileNav.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', () => {
            mobileNav.classList.add('hidden');
            mobileNav.classList.remove('flex');
            document.body.style.overflow = '';
        });
    });
}
