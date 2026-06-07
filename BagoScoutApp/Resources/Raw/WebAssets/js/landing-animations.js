// Professional GSAP Animations for BagoScout Landing Page
gsap.registerPlugin(ScrollTrigger);

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', () => {
    // Initial page load animations - DON'T animate nav to preserve sticky
    gsap.from('.landing-nav', {
        opacity: 0,
        duration: 0.6,
        ease: 'power2.out',
        clearProps: 'all' // Clear all properties after animation
    });

    // Hero content animations
    gsap.from('.hero-title', {
        x: -30,
        opacity: 0,
        duration: 0.7,
        delay: 0.2,
        ease: 'power2.out'
    });

    gsap.from('.hero-description', {
        x: -30,
        opacity: 0,
        duration: 0.7,
        delay: 0.3,
        ease: 'power2.out'
    });

    gsap.from('.search-box', {
        y: 20,
        opacity: 0,
        duration: 0.7,
        delay: 0.4,
        ease: 'power2.out'
    });

    gsap.from('.hero-features .feature-item', {
        y: 20,
        opacity: 0,
        duration: 0.7,
        delay: 0.5,
        stagger: 0.15,
        ease: 'power2.out'
    });

    // Hero image fade in
    gsap.from('.hero-image', {
        x: 30,
        opacity: 0,
        duration: 0.8,
        delay: 0.4,
        ease: 'power2.out'
    });
});

// Scroll-triggered animations for features section
gsap.from('.features-section .section-label', {
    scrollTrigger: {
        trigger: '.features-section',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 15,
    opacity: 0,
    duration: 0.6,
    ease: 'power2.out'
});

gsap.from('.features-section h2', {
    scrollTrigger: {
        trigger: '.features-section',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 15,
    opacity: 0,
    duration: 0.6,
    delay: 0.15,
    ease: 'power2.out'
});

// Feature cards animation - ensure they're visible
const featureCards = document.querySelectorAll('.feature-card');
featureCards.forEach(card => {
    card.style.opacity = '1';
    card.style.visibility = 'visible';
});

gsap.from('.feature-card', {
    scrollTrigger: {
        trigger: '.features-grid',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 30,
    opacity: 0,
    duration: 0.7,
    stagger: 0.15,
    ease: 'power2.out',
    clearProps: 'all'
});

// Mission & Vision animations
gsap.from('.mission-item h2', {
    scrollTrigger: {
        trigger: '.mission-item',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    x: -25,
    opacity: 0,
    duration: 0.7,
    ease: 'power2.out'
});

gsap.from('.mission-content', {
    scrollTrigger: {
        trigger: '.mission-item',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    x: 25,
    opacity: 0,
    duration: 0.7,
    delay: 0.15,
    ease: 'power2.out'
});

gsap.from('.vision-item h2', {
    scrollTrigger: {
        trigger: '.vision-item',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    x: -25,
    opacity: 0,
    duration: 0.7,
    ease: 'power2.out'
});

gsap.from('.vision-content', {
    scrollTrigger: {
        trigger: '.vision-item',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    x: 25,
    opacity: 0,
    duration: 0.7,
    delay: 0.15,
    ease: 'power2.out'
});

// Contact section animations
gsap.from('.contact-section .section-label', {
    scrollTrigger: {
        trigger: '.contact-section',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 15,
    opacity: 0,
    duration: 0.6,
    ease: 'power2.out'
});

gsap.from('.contact-section h2', {
    scrollTrigger: {
        trigger: '.contact-section',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 15,
    opacity: 0,
    duration: 0.6,
    delay: 0.15,
    ease: 'power2.out'
});

// Contact cards animation - ensure they're visible
const contactCards = document.querySelectorAll('.contact-card');
contactCards.forEach(card => {
    card.style.opacity = '1';
    card.style.visibility = 'visible';
});

gsap.from('.contact-card', {
    scrollTrigger: {
        trigger: '.contact-grid',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 30,
    opacity: 0,
    duration: 0.7,
    stagger: 0.12,
    ease: 'power2.out',
    clearProps: 'all'
});

// Message form animations
gsap.from('.message-section .section-label', {
    scrollTrigger: {
        trigger: '.message-section',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 15,
    opacity: 0,
    duration: 0.6,
    ease: 'power2.out'
});

gsap.from('.message-section h2', {
    scrollTrigger: {
        trigger: '.message-section',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 15,
    opacity: 0,
    duration: 0.6,
    delay: 0.15,
    ease: 'power2.out'
});

gsap.from('.message-form', {
    scrollTrigger: {
        trigger: '.message-form',
        start: 'top 80%',
        toggleActions: 'play none none none'
    },
    y: 25,
    opacity: 0,
    duration: 0.7,
    ease: 'power2.out'
});

// Footer animation
gsap.from('.footer-column', {
    scrollTrigger: {
        trigger: '.landing-footer',
        start: 'top 90%',
        toggleActions: 'play none none none'
    },
    y: 25,
    opacity: 0,
    duration: 0.7,
    stagger: 0.1,
    ease: 'power2.out'
});

// Smooth hover effects for buttons
document.querySelectorAll('.btn-get-started, .btn-login, .btn-create, .btn-submit').forEach(button => {
    button.addEventListener('mouseenter', () => {
        gsap.to(button, {
            scale: 1.03,
            duration: 0.3,
            ease: 'power2.out'
        });
    });
    
    button.addEventListener('mouseleave', () => {
        gsap.to(button, {
            scale: 1,
            duration: 0.3,
            ease: 'power2.out'
        });
    });
});
