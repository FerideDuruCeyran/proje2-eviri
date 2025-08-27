function togglePassword(inputId, iconId) {
    const passwordInput = document.getElementById(inputId);
    const passwordIcon = document.getElementById(iconId);
    
    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        passwordIcon.classList.remove('bi-eye');
        passwordIcon.classList.add('bi-eye-slash');
    } else {
        passwordInput.type = 'password';
        passwordIcon.classList.remove('bi-eye-slash');
        passwordIcon.classList.add('bi-eye');
    }
}

// Form validation for login
function validateLoginForm() {
    const form = document.querySelector('form');
    form.addEventListener('submit', function(e) {
        const email = document.querySelector('input[name="Email"]').value.trim();
        const password = document.querySelector('input[name="Password"]').value.trim();
        
        if (!email || !password) {
            e.preventDefault();
            alert("Lütfen tüm alanları doldurun!");
            return false;
        }
        
        if (!email.includes('@')) {
            e.preventDefault();
            alert("Lütfen geçerli bir e-posta adresi girin!");
            return false;
        }
    });
}

// Form validation for register
function validateRegisterForm() {
    const passwordInput = document.getElementById('passwordInput');
    const confirmPasswordInput = document.getElementById('confirmPasswordInput');
    const registerBtn = document.getElementById('registerBtn');
    const termsCheck = document.getElementById('termsCheck');

    function validateForm() {
        const password = passwordInput.value;
        const confirmPassword = confirmPasswordInput.value;
        const termsAccepted = termsCheck.checked;
        
        let isValid = true;
        
        // Password validation
        if (password.length < 6) {
            isValid = false;
        }
        
        // Confirm password validation
        if (password !== confirmPassword) {
            isValid = false;
        }
        
        // Terms validation
        if (!termsAccepted) {
            isValid = false;
        }
        
        registerBtn.disabled = !isValid;
        return isValid;
    }

    // Add event listeners
    passwordInput.addEventListener('input', validateForm);
    confirmPasswordInput.addEventListener('input', validateForm);
    termsCheck.addEventListener('change', validateForm);

    // Initial validation
    validateForm();

    // Form submission validation
    const form = document.querySelector('form');
    form.addEventListener('submit', function(e) {
        if (!validateForm()) {
            e.preventDefault();
            alert("Lütfen tüm alanları doğru şekilde doldurun ve şartları kabul edin!");
            return false;
        }
        
        // Additional validation
        const firstName = document.querySelector('input[name="FirstName"]').value.trim();
        const lastName = document.querySelector('input[name="LastName"]').value.trim();
        const email = document.querySelector('input[name="Email"]').value.trim();
        
        if (!firstName || !lastName || !email) {
            e.preventDefault();
            alert("Lütfen tüm zorunlu alanları doldurun!");
            return false;
        }
        
        if (!email.includes('@')) {
            e.preventDefault();
            alert("Lütfen geçerli bir e-posta adresi girin!");
            return false;
        }
    });
}

// Initialize forms when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on login page
    if (document.querySelector('input[name="Email"]') && document.querySelector('input[name="Password"]')) {
        validateLoginForm();
    }
    
    // Check if we're on register page
    if (document.getElementById('passwordInput') && document.getElementById('confirmPasswordInput')) {
        validateRegisterForm();
    }
});
