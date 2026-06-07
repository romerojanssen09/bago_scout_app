// Login Modal JavaScript

// Modal Control
function openLoginModal() {
    document.getElementById('loginModal').classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeLoginModal() {
    document.getElementById('loginModal').classList.remove('active');
    document.body.style.overflow = '';
}

function openRegisterModal() {
    window.location.href = '/Home/Register';
}

// Close modal when clicking overlay
document.addEventListener('click', function(e) {
    if (e.target.classList.contains('modal-overlay')) {
        closeLoginModal();
    }
});

// Check if should open login modal on page load
document.addEventListener('DOMContentLoaded', function() {
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('login') === 'true') {
        openLoginModal();
        // Clean up URL without reloading page
        const url = new URL(window.location);
        url.searchParams.delete('login');
        window.history.replaceState({}, '', url);
    }

    // Attach login form event listener
    const loginForm = document.getElementById('loginForm');
    
    if (loginForm) {
        loginForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const email = document.getElementById('loginEmail').value;
            const password = document.getElementById('loginPassword').value;
            const rememberMe = document.getElementById('rememberMe')?.checked || false;
            
            if (!email || !password) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Missing Information',
                    text: 'Please enter both email and password',
                    confirmButtonColor: '#1C2B53'
                });
                return;
            }
            
            try {
                const response = await fetch('/api/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    credentials: 'include',
                    body: JSON.stringify({ email, password, rememberMe })
                });

                const data = await response.json();

                if (data.success) {
                    closeLoginModal();
                    
                    Swal.fire({
                        icon: 'success',
                        title: 'Welcome Back!',
                        text: `Logging you in...`,
                        confirmButtonColor: '#1C2B53',
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        if (data.userType === 'seeker') {
                            window.location.href = '/Dashboard/Seeker';
                        } else {
                            window.location.href = '/Dashboard/Employer';
                        }
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Login Failed',
                        text: 'Invalid username or password. Please try again.',
                        confirmButtonColor: '#1C2B53'
                    });
                }
            } catch (error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'An error occurred during login. Please try again.',
                    confirmButtonColor: '#1C2B53'
                });
            }
        });
    }
});

// Password Toggle
function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    const button = event.currentTarget;
    const icon = button.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
}
