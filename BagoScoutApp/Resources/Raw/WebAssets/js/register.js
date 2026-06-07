// Registration Page JavaScript
let currentStep = 1;
let userType = '';
let registrationData = {
    userType: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phone: '',
    verificationCode: '',
    selfiePhoto: null,
    idPhoto: null,
    skills: [],
    preferences: []
};

// Show Step
function showStep(step) {
    currentStep = step;
    
    // Hide all steps
    for (let i = 1; i <= 5; i++) {
        const stepContent = document.getElementById(`step${i}`);
        const stepIndicator = document.getElementById(`stepIndicator${i}`);
        
        if (stepContent) stepContent.classList.remove('active');
        if (stepIndicator) stepIndicator.classList.remove('active');
    }
    
    // Show current step
    const currentStepContent = document.getElementById(`step${step}`);
    const currentStepIndicator = document.getElementById(`stepIndicator${step}`);
    
    if (currentStepContent) currentStepContent.classList.add('active');
    if (currentStepIndicator) currentStepIndicator.classList.add('active');
    
    // Mark completed steps
    for (let i = 1; i < step; i++) {
        const indicator = document.getElementById(`stepIndicator${i}`);
        if (indicator) indicator.classList.add('completed');
    }
    
    // Auto-scroll step indicator on mobile
    scrollToActiveStep(step);
    
    // Update buttons
    updateNavigationButtons();
}

// Auto-scroll step indicator to show active step
function scrollToActiveStep(step) {
    const stepIndicator = document.querySelector('.step-indicator');
    const activeStep = document.getElementById(`stepIndicator${step}`);
    
    if (stepIndicator && activeStep && window.innerWidth <= 640) {
        // Calculate scroll position to center the active step
        const stepIndicatorRect = stepIndicator.getBoundingClientRect();
        const activeStepRect = activeStep.getBoundingClientRect();
        
        const scrollLeft = activeStep.offsetLeft - (stepIndicatorRect.width / 2) + (activeStepRect.width / 2);
        
        stepIndicator.scrollTo({
            left: scrollLeft,
            behavior: 'smooth'
        });
    }
}

async function nextStep() {
    if (await validateCurrentStep()) {
        if (currentStep < 5) {
            showStep(currentStep + 1);
        } else {
            await completeRegistration();
        }
    }
}

function previousStep() {
    if (currentStep > 1) {
        showStep(currentStep - 1);
    }
}

function updateNavigationButtons() {
    const backBtn = document.getElementById('backBtn');
    const nextBtn = document.getElementById('nextBtn');
    
    if (backBtn) {
        backBtn.style.display = currentStep === 1 ? 'none' : 'block';
    }
    
    if (nextBtn) {
        nextBtn.textContent = currentStep === 5 ? 'Complete Registration' : 'Next';
    }
}

async function validateCurrentStep() {
    switch(currentStep) {
        case 1:
            if (!registrationData.userType) {
                Swal.fire({
                    icon: 'warning',
                    title: 'User Type Required',
                    text: 'Please select whether you are a Job Seeker or Employer',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            return true;
        case 2:
            const email = document.getElementById('regEmail').value;
            const password = document.getElementById('regPassword').value;
            const confirmPassword = document.getElementById('regConfirmPassword').value;
            const firstName = document.getElementById('regFirstName').value;
            const lastName = document.getElementById('regLastName').value;
            
            if (!email || !password || !confirmPassword || !firstName || !lastName) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Missing Information',
                    text: 'Please fill in all fields',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            
            // Validate email format
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(email)) {
                Swal.fire({
                    icon: 'error',
                    title: 'Invalid Email',
                    text: 'Please enter a valid email address',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            
            // Check if email already exists
            showLoading();
            document.querySelector('.loading-text').textContent = 'Checking email...';
            document.querySelector('.loading-subtext').textContent = 'Please wait';
            
            try {
                const checkResponse = await fetch(`/api/auth/check-email/${encodeURIComponent(email)}`);
                const checkData = await checkResponse.json();
                
                hideLoading();
                
                if (checkData.exists) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Email Already Registered',
                        text: 'This email is already registered. Please use a different email or login.',
                        confirmButtonColor: '#1C2B53'
                    });
                    return false;
                }
            } catch (error) {
                hideLoading();
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Unable to verify email. Please try again.',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            
            // Validate password requirements
            const passwordValidation = validatePassword(password);
            if (!passwordValidation.valid) {
                Swal.fire({
                    icon: 'error',
                    title: 'Invalid Password',
                    html: 'Password must meet all requirements:<br>' + passwordValidation.errors.join('<br>'),
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            
            if (password !== confirmPassword) {
                Swal.fire({
                    icon: 'error',
                    title: 'Passwords Don\'t Match',
                    text: 'Please make sure both passwords are identical',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            
            registrationData.email = email;
            registrationData.password = password;
            registrationData.firstName = firstName;
            registrationData.lastName = lastName;
            
            // Send verification code
            await sendVerificationCode(email);
            return true;
        case 3:
            const code = getVerificationCode();
            if (code.length !== 6) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Incomplete Code',
                    text: 'Please enter the complete 6-digit verification code',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            registrationData.verificationCode = code;
            
            // Verify the code with backend
            const verified = await verifyEmailCode(registrationData.email, code);
            if (!verified) {
                Swal.fire({
                    icon: 'error',
                    title: 'Invalid Code',
                    text: 'The verification code is invalid or has expired',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            return true;
        case 4:
            if (!registrationData.selfiePhoto || !registrationData.idPhoto) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Documents Required',
                    text: 'Please capture your selfie and upload your ID',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            return true;
        case 5:
            if (registrationData.userType === 'seeker' && registrationData.skills.length === 0) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Skills Required',
                    text: 'Please select at least one skill',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            if (registrationData.userType === 'employer' && registrationData.preferences.length === 0) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Preferences Required',
                    text: 'Please select at least one skill preference',
                    confirmButtonColor: '#1C2B53'
                });
                return false;
            }
            return true;
        default:
            return true;
    }
}

// Password validation function
function validatePassword(password) {
    const errors = [];
    let valid = true;
    
    if (password.length < 8) {
        errors.push('• At least 8 characters');
        valid = false;
    }
    if (!/[A-Z]/.test(password)) {
        errors.push('• One uppercase letter');
        valid = false;
    }
    if (!/[0-9]/.test(password)) {
        errors.push('• One number');
        valid = false;
    }
    if (!/[!@#$%^&*(),.?":{}|<>+=_\-\[\]\\\/~`';]/.test(password)) {
        errors.push('• One special character');
        valid = false;
    }
    
    return { valid, errors };
}

// Step 1: User Type Selection
function selectUserType(type) {
    registrationData.userType = type;
    userType = type;
    
    document.querySelectorAll('.user-type-card').forEach(card => {
        card.classList.remove('selected');
    });
    
    event.target.closest('.user-type-card').classList.add('selected');
    
    // Update step 5 content based on user type
    updateStep5Content(type);
}

function updateStep5Content(type) {
    const step5Content = document.getElementById('step5Content');
    
    if (type === 'seeker') {
        step5Content.innerHTML = `
            <h3 class="step-title">Select Your Skills</h3>
            <p class="step-subtitle">Choose skills that match your expertise</p>
            <div class="skills-grid">
                <div class="skill-tag" onclick="toggleSkill('Web Development')">Web Development</div>
                <div class="skill-tag" onclick="toggleSkill('Mobile Development')">Mobile Development</div>
                <div class="skill-tag" onclick="toggleSkill('Graphic Design')">Graphic Design</div>
                <div class="skill-tag" onclick="toggleSkill('Data Entry')">Data Entry</div>
                <div class="skill-tag" onclick="toggleSkill('Customer Service')">Customer Service</div>
                <div class="skill-tag" onclick="toggleSkill('Sales')">Sales</div>
                <div class="skill-tag" onclick="toggleSkill('Marketing')">Marketing</div>
                <div class="skill-tag" onclick="toggleSkill('Accounting')">Accounting</div>
            </div>
            <div class="custom-skill-input">
                <input type="text" id="customSkill" class="form-input" placeholder="Add custom skill">
                <button onclick="addCustomSkill()" class="btn-secondary" style="margin-top: 10px;">Add Skill</button>
            </div>
            <div class="added-skills-container">
                <div class="added-skills-title">Added Skills:</div>
                <div id="addedSkillsList" class="added-skills-list">
                    <div class="empty-skills-message">No skills added yet. Select or add skills above.</div>
                </div>
            </div>
        `;
    } else {
        step5Content.innerHTML = `
            <h3 class="step-title">Select Required Skills</h3>
            <p class="step-subtitle">Choose skills you're looking for in candidates</p>
            <div class="skills-grid">
                <div class="skill-tag" onclick="togglePreference('Web Development')">Web Development</div>
                <div class="skill-tag" onclick="togglePreference('Mobile Development')">Mobile Development</div>
                <div class="skill-tag" onclick="togglePreference('Graphic Design')">Graphic Design</div>
                <div class="skill-tag" onclick="togglePreference('Data Entry')">Data Entry</div>
                <div class="skill-tag" onclick="togglePreference('Customer Service')">Customer Service</div>
                <div class="skill-tag" onclick="togglePreference('Sales')">Sales</div>
                <div class="skill-tag" onclick="togglePreference('Marketing')">Marketing</div>
                <div class="skill-tag" onclick="togglePreference('Accounting')">Accounting</div>
            </div>
            <div class="custom-skill-input">
                <input type="text" id="customPreference" class="form-input" placeholder="Add custom requirement">
                <button onclick="addCustomPreference()" class="btn-secondary" style="margin-top: 10px;">Add Requirement</button>
            </div>
            <div class="added-skills-container">
                <div class="added-skills-title">Required Skills:</div>
                <div id="addedPreferencesList" class="added-skills-list">
                    <div class="empty-skills-message">No requirements added yet. Select or add requirements above.</div>
                </div>
            </div>
        `;
    }
    
    // Update the display if there are already skills/preferences
    if (type === 'seeker' && registrationData.skills.length > 0) {
        updateAddedSkillsDisplay();
    } else if (type === 'employer' && registrationData.preferences.length > 0) {
        updateAddedPreferencesDisplay();
    }
}

// Step 3: Verification Code
async function sendVerificationCode(email) {
    // Show loading overlay
    showLoading();
    
    try {
        const response = await fetch('/api/auth/send-verification-code', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                email: email,
                name: registrationData.firstName 
            })
        });

        const data = await response.json();

        // Hide loading overlay
        hideLoading();

        if (data.success) {
            Swal.fire({
                icon: 'success',
                title: 'Code Sent!',
                text: `Verification code sent to ${email}. Please check your email.`,
                confirmButtonColor: '#1C2B53'
            });
            console.log('Verification code:', data.code); // For testing
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Failed to Send',
                text: data.message || 'Failed to send verification code',
                confirmButtonColor: '#1C2B53'
            });
            return false;
        }
    } catch (error) {
        // Hide loading overlay
        hideLoading();
        console.error('Error sending verification code:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Error sending verification code. Please try again.',
            confirmButtonColor: '#1C2B53'
        });
        return false;
    }
}

// Show/Hide Loading Overlay
function showLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.add('active');
    }
}

function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.remove('active');
    }
}

function getVerificationCode() {
    let code = '';
    for (let i = 1; i <= 6; i++) {
        const input = document.getElementById(`code${i}`);
        code += input ? input.value : '';
    }
    return code;
}

// Auto-focus next input and handle paste
document.addEventListener('DOMContentLoaded', function() {
    const codeInputs = document.querySelectorAll('.verification-code input');
    
    codeInputs.forEach((input, index) => {
        // Handle input
        input.addEventListener('input', function(e) {
            // Only allow numbers
            this.value = this.value.replace(/[^0-9]/g, '');
            
            if (this.value.length === 1 && index < codeInputs.length - 1) {
                codeInputs[index + 1].focus();
            }
        });
        
        // Handle backspace
        input.addEventListener('keydown', function(e) {
            if (e.key === 'Backspace' && this.value === '' && index > 0) {
                codeInputs[index - 1].focus();
            }
        });
        
        // Handle paste - spread numbers across inputs
        input.addEventListener('paste', function(e) {
            e.preventDefault();
            const pastedData = e.clipboardData.getData('text').replace(/[^0-9]/g, '');
            
            if (pastedData.length > 0) {
                // Spread the pasted numbers across all inputs starting from current position
                for (let i = 0; i < pastedData.length && (index + i) < codeInputs.length; i++) {
                    codeInputs[index + i].value = pastedData[i];
                }
                
                // Focus on the next empty input or the last one
                const nextIndex = Math.min(index + pastedData.length, codeInputs.length - 1);
                codeInputs[nextIndex].focus();
            }
        });
    });
    
    // Password validation
    const passwordInput = document.getElementById('regPassword');
    const confirmPasswordInput = document.getElementById('regConfirmPassword');
    
    if (passwordInput) {
        passwordInput.addEventListener('input', function() {
            checkPasswordRequirements(this.value);
            checkPasswordMatch();
        });
    }
    
    if (confirmPasswordInput) {
        confirmPasswordInput.addEventListener('input', function() {
            checkPasswordMatch();
        });
    }
});

// Check password requirements
function checkPasswordRequirements(password) {
    const requirements = {
        'req-length': password.length >= 8,
        'req-capital': /[A-Z]/.test(password),
        'req-number': /[0-9]/.test(password),
        'req-special': /[!@#$%^&*(),.?":{}|<>+=_\-\[\]\\\/~`';]/.test(password)
    };
    
    for (const [id, met] of Object.entries(requirements)) {
        const element = document.getElementById(id);
        if (element) {
            if (met) {
                element.classList.add('met');
            } else {
                element.classList.remove('met');
            }
        }
    }
}

// Check password match
function checkPasswordMatch() {
    const password = document.getElementById('regPassword').value;
    const confirmPassword = document.getElementById('regConfirmPassword').value;
    const matchElement = document.getElementById('password-match');
    
    if (!matchElement) return;
    
    if (confirmPassword === '') {
        matchElement.textContent = '';
        matchElement.className = 'password-match';
    } else if (password === confirmPassword) {
        matchElement.textContent = '✓ Passwords match';
        matchElement.className = 'password-match match';
    } else {
        matchElement.textContent = '✗ Passwords do not match';
        matchElement.className = 'password-match no-match';
    }
}

async function resendCode() {
    await sendVerificationCode(registrationData.email);
}

// Verify email code with backend
async function verifyEmailCode(email, code) {
    try {
        const response = await fetch('/api/auth/verify-email', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, code })
        });

        const data = await response.json();
        return data.success;
    } catch (error) {
        console.error('Error verifying code:', error);
        return false;
    }
}

// Password Toggle
function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    const icon = event.target;
    
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

// Step 4: Camera and File Upload
let stream = null;

async function startCamera() {
    try {
        stream = await navigator.mediaDevices.getUserMedia({ video: true });
        const video = document.getElementById('cameraVideo');
        video.srcObject = stream;
        video.classList.add('active');
        video.play();
        
        document.getElementById('startCameraBtn').style.display = 'none';
        document.getElementById('captureBtn').style.display = 'block';
    } catch (error) {
        Swal.fire({
            icon: 'error',
            title: 'Camera Access Denied',
            text: 'Unable to access camera: ' + error.message,
            confirmButtonColor: '#1C2B53'
        });
    }
}

function capturePhoto() {
    const video = document.getElementById('cameraVideo');
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d').drawImage(video, 0, 0);
    
    canvas.toBlob(blob => {
        registrationData.selfiePhoto = blob;
        
        const preview = document.getElementById('selfiePreview');
        preview.innerHTML = `<img src="${canvas.toDataURL()}" alt="Selfie">`;
        preview.classList.add('active');
        
        stopCamera();
        document.getElementById('captureBtn').style.display = 'none';
        document.getElementById('retakeBtn').style.display = 'block';
    });
}

function retakePhoto() {
    registrationData.selfiePhoto = null;
    document.getElementById('selfiePreview').classList.remove('active');
    document.getElementById('retakeBtn').style.display = 'none';
    document.getElementById('startCameraBtn').style.display = 'block';
}

function stopCamera() {
    if (stream) {
        stream.getTracks().forEach(track => track.stop());
        document.getElementById('cameraVideo').classList.remove('active');
    }
}

function uploadID() {
    document.getElementById('idUpload').click();
}

document.addEventListener('DOMContentLoaded', function() {
    const idUpload = document.getElementById('idUpload');
    if (idUpload) {
        idUpload.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                registrationData.idPhoto = file;
                
                const reader = new FileReader();
                reader.onload = function(e) {
                    const preview = document.getElementById('idPreview');
                    preview.innerHTML = `<img src="${e.target.result}" alt="ID"><p style="color: #10B981; margin-top: 10px; font-weight: 600;"><i class="fas fa-check-circle"></i> ID uploaded successfully</p>`;
                    preview.classList.add('active');
                };
                reader.readAsDataURL(file);
            }
        });
    }
});

// Step 5: Skills/Preferences
function toggleSkill(skill) {
    const index = registrationData.skills.indexOf(skill);
    if (index > -1) {
        registrationData.skills.splice(index, 1);
        event.target.classList.remove('selected');
    } else {
        registrationData.skills.push(skill);
        event.target.classList.add('selected');
    }
    updateAddedSkillsDisplay();
}

function addCustomSkill() {
    const input = document.getElementById('customSkill');
    const skill = input.value.trim();
    if (skill) {
        registrationData.skills.push(skill);
        input.value = '';
        updateAddedSkillsDisplay();
    }
}

function updateAddedSkillsDisplay() {
    const container = document.getElementById('addedSkillsList');
    if (!container) return;
    
    if (registrationData.skills.length === 0) {
        container.innerHTML = '<div class="empty-skills-message">No skills added yet. Select or add skills above.</div>';
    } else {
        container.innerHTML = registrationData.skills.map(skill => `
            <div class="added-skill-item">
                <span>${skill}</span>
                <span class="remove-skill" onclick="removeSkill('${skill}')">
                    <i class="fas fa-times"></i>
                </span>
            </div>
        `).join('');
    }
}

function removeSkill(skill) {
    const index = registrationData.skills.indexOf(skill);
    if (index > -1) {
        registrationData.skills.splice(index, 1);
        updateAddedSkillsDisplay();
        
        // Also unselect the tag if it exists
        const tags = document.querySelectorAll('.skill-tag');
        tags.forEach(tag => {
            if (tag.textContent === skill) {
                tag.classList.remove('selected');
            }
        });
    }
}

function togglePreference(pref) {
    const index = registrationData.preferences.indexOf(pref);
    if (index > -1) {
        registrationData.preferences.splice(index, 1);
        event.target.classList.remove('selected');
    } else {
        registrationData.preferences.push(pref);
        event.target.classList.add('selected');
    }
    updateAddedPreferencesDisplay();
}

function addCustomPreference() {
    const input = document.getElementById('customPreference');
    const pref = input.value.trim();
    if (pref) {
        registrationData.preferences.push(pref);
        input.value = '';
        updateAddedPreferencesDisplay();
    }
}

function updateAddedPreferencesDisplay() {
    const container = document.getElementById('addedPreferencesList');
    if (!container) return;
    
    if (registrationData.preferences.length === 0) {
        container.innerHTML = '<div class="empty-skills-message">No requirements added yet. Select or add requirements above.</div>';
    } else {
        container.innerHTML = registrationData.preferences.map(pref => `
            <div class="added-skill-item">
                <span>${pref}</span>
                <span class="remove-skill" onclick="removePreference('${pref}')">
                    <i class="fas fa-times"></i>
                </span>
            </div>
        `).join('');
    }
}

function removePreference(pref) {
    const index = registrationData.preferences.indexOf(pref);
    if (index > -1) {
        registrationData.preferences.splice(index, 1);
        updateAddedPreferencesDisplay();
        
        // Also unselect the tag if it exists
        const tags = document.querySelectorAll('.skill-tag');
        tags.forEach(tag => {
            if (tag.textContent === pref) {
                tag.classList.remove('selected');
            }
        });
    }
}

// Complete Registration
async function completeRegistration() {
    // Show loading overlay
    showLoading();
    document.querySelector('.loading-text').textContent = 'Creating your account...';
    document.querySelector('.loading-subtext').textContent = 'This will only take a moment';
    
    try {
        // Step 1: Register the user
        const registerResponse = await fetch('/api/auth/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                firstName: registrationData.firstName,
                lastName: registrationData.lastName,
                email: registrationData.email,
                password: registrationData.password,
                userType: registrationData.userType
            })
        });

        const registerData = await registerResponse.json();

        if (!registerData.success) {
            hideLoading();
            Swal.fire({
                icon: 'error',
                title: 'Registration Failed',
                text: registerData.message || 'An error occurred during registration',
                confirmButtonColor: '#1C2B53'
            });
            return;
        }

        const userId = registerData.userId;

        // Step 2: Upload photos
        document.querySelector('.loading-text').textContent = 'Uploading photos...';
        
        const formData = new FormData();
        formData.append('UserId', userId);
        
        if (registrationData.selfiePhoto) {
            formData.append('SelfiePhoto', registrationData.selfiePhoto, 'selfie.jpg');
        }
        
        if (registrationData.idPhoto) {
            formData.append('IdPhoto', registrationData.idPhoto, registrationData.idPhoto.name);
        }

        await fetch('/api/auth/upload-photos', {
            method: 'POST',
            body: formData
        });

        // Step 3: Save skills/preferences
        document.querySelector('.loading-text').textContent = 'Saving your preferences...';
        
        const skills = registrationData.userType === 'seeker' 
            ? registrationData.skills 
            : registrationData.preferences;

        if (skills.length > 0) {
            await fetch('/api/auth/save-skills', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    userId: userId,
                    skills: skills
                })
            });
        }

        // Hide loading overlay
        hideLoading();

        // Show success message with SweetAlert2
        Swal.fire({
            icon: 'success',
            title: 'Registration Successful!',
            text: 'Your account has been created and verified. Redirecting to login...',
            confirmButtonColor: '#1C2B53',
            timer: 2000,
            showConfirmButton: false
        }).then(() => {
            window.location.href = '/?login=true';
        });
    } catch (error) {
        // Hide loading overlay
        hideLoading();
        console.error('Error during registration:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'An error occurred during registration. Please try again.',
            confirmButtonColor: '#1C2B53'
        });
    }
}

// Open login modal (redirect to home and open modal)
function openLoginModal() {
    window.location.href = '/?login=true';
}
