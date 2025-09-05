// Global variables
let currentUser = null;
let isAuthenticated = false;

// Initialize application
document.addEventListener('DOMContentLoaded', function() {
    console.log('Excel Uploader Application Initialized');
    initializeApp();
});

// Initialize application
function initializeApp() {
    console.log('Initializing application...');
    
    // Check authentication status
    checkAuthStatus();
    
    // Setup event listeners
    setupEventListeners();
    
    // Setup navigation
    setupNavigation();
}

// Check authentication status
function checkAuthStatus() {
    console.log('Checking authentication status...');
    const token = localStorage.getItem('authToken');
    
    if (token) {
        console.log('Token found, verifying...');
        verifyToken(token);
    } else {
        console.log('No token found, user not authenticated');
        setUnauthenticated();
    }
}

// Verify token with server
async function verifyToken(token) {
    try {
        console.log('Verifying token...');
        const response = await fetch('/api/account/verify-token', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const userData = await response.json();
            console.log('Token verified successfully:', userData);
            
            // Set authenticated state
            isAuthenticated = true;
            currentUser = userData.user || userData;
            
            // Update UI
            setAuthenticated();
        } else {
            console.log('Token verification failed');
            setUnauthenticated();
        }
    } catch (error) {
        console.error('Token verification error:', error);
        setUnauthenticated();
    }
}

// Set authenticated state
function setAuthenticated() {
    console.log('Setting authenticated state');
    isAuthenticated = true;
    document.body.classList.add('authenticated');
    updateNavigation();
}

// Set unauthenticated state
function setUnauthenticated() {
    console.log('Setting unauthenticated state');
    isAuthenticated = false;
    currentUser = null;
    localStorage.removeItem('authToken');
    document.body.classList.remove('authenticated');
    updateNavigation();
}

// Update navigation based on authentication status
function updateNavigation() {
    console.log('Updating navigation, isAuthenticated:', isAuthenticated);
    
    if (isAuthenticated && currentUser) {
        // Update user name
        const userNameElements = document.querySelectorAll('#userName');
        userNameElements.forEach(el => {
            el.textContent = `${currentUser.firstName} ${currentUser.lastName}`;
        });
        
        console.log('Navigation updated for authenticated user');
    } else {
        // Clear user name
        const userNameElements = document.querySelectorAll('#userName');
        userNameElements.forEach(el => {
            el.textContent = 'Profil';
        });
        
        console.log('Navigation updated for unauthenticated user');
    }
}

// Setup event listeners
function setupEventListeners() {
    console.log('Setting up event listeners...');
    
    // Login form
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        console.log('Login form found, adding event listener');
        loginForm.addEventListener('submit', handleLogin);
    }
    
    // Register form
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        console.log('Register form found, adding event listener');
        registerForm.addEventListener('submit', handleRegister);
    }
    
    // Upload form
    const uploadForm = document.getElementById('uploadForm');
    if (uploadForm) {
        console.log('Upload form found, adding event listener');
        uploadForm.addEventListener('submit', handleFormUpload);
    }
    
    // File upload
    setupFileUpload();
}

// Setup navigation
function setupNavigation() {
    console.log('Setting up navigation...');
    
    // Get current page path
    const currentPath = window.location.pathname;
    console.log('Current path:', currentPath);
    
    // Remove all active classes from navigation
    const allNavLinks = document.querySelectorAll('.nav-link');
    allNavLinks.forEach(link => {
        link.classList.remove('active');
    });
    
    // Add active class to current page
    const currentNavLink = document.querySelector(`.nav-link[href="${currentPath}"]`);
    if (currentNavLink) {
        currentNavLink.classList.add('active');
        console.log('Set active navigation:', currentPath);
    }
    
    // Add click event listeners to navigation links
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            const href = this.getAttribute('href');
            
            // Skip if it's a logout link or already on the same page
            if (href === '#' || href === currentPath) {
                return;
            }
            
            // Check if it's an auth-only link and user is not authenticated
            if (this.closest('.auth-only') && !isAuthenticated) {
                e.preventDefault();
                console.log('User not authenticated, redirecting to login');
                window.location.href = '/login';
                return;
            }
            
            // Allow navigation for unauth-only links or authenticated users
            console.log('Navigating to:', href);
        });
    });
}

// Handle login
async function handleLogin(e) {
    console.log('handleLogin called');
    e.preventDefault();
    
    const formData = new FormData(e.target);
    const email = formData.get('email');
    const password = formData.get('password');
    const rememberMe = formData.get('rememberMe') === 'on' || formData.get('rememberMe') === 'true';

    console.log('Login attempt:', { email, password: '***', rememberMe });

    try {
        const response = await fetch('/api/account/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password, rememberMe })
        });

        console.log('Login response status:', response.status);
        
        if (response.ok) {
            const result = await response.json();
            console.log('Login successful:', result);
            
            // Store token and user data
            localStorage.setItem('authToken', result.token);
            isAuthenticated = true;
            currentUser = result.user;
            
            // Update UI
            setAuthenticated();
            
            showAlert('Başarıyla giriş yapıldı!', 'success');
            
            // Redirect to home page
            console.log('Redirecting to home page...');
            window.location.href = '/home';
        } else {
            const error = await response.json();
            console.error('Login failed:', error);
            
            let errorMessage = 'Giriş başarısız';
            if (error.message) {
                errorMessage = error.message;
            }
            if (error.errors && Array.isArray(error.errors)) {
                errorMessage += ': ' + error.errors.join(', ');
            }
            
            showAlert(errorMessage, 'danger');
        }
    } catch (error) {
        console.error('Login error:', error);
        showAlert('Giriş yapılırken hata oluştu', 'danger');
    }
}

// Handle register
async function handleRegister(e) {
    console.log('handleRegister called');
    e.preventDefault();
    
    const formData = new FormData(e.target);
    const userData = {
        firstName: formData.get('firstName'),
        lastName: formData.get('lastName'),
        email: formData.get('email'),
        password: formData.get('password'),
        confirmPassword: formData.get('confirmPassword')
    };

    if (userData.password !== userData.confirmPassword) {
        showAlert('Şifreler eşleşmiyor', 'danger');
        return;
    }

    console.log('Register attempt:', { ...userData, password: '***', confirmPassword: '***' });

    try {
        const response = await fetch('/api/account/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userData)
        });

        if (response.ok) {
            showAlert('Kayıt başarılı! Giriş yapabilirsiniz.', 'success');
            setTimeout(() => {
                window.location.href = '/login';
            }, 1000);
        } else {
            const error = await response.json();
            showAlert(error.message || 'Kayıt başarısız', 'danger');
        }
    } catch (error) {
        console.error('Register error:', error);
        showAlert('Kayıt olurken hata oluştu', 'danger');
    }
}

// Handle form upload
async function handleFormUpload(e) {
    console.log('handleFormUpload called');
    e.preventDefault();
    
    if (!isAuthenticated) {
        showAlert('Lütfen önce giriş yapın', 'warning');
        return;
    }
    
    const formData = new FormData(e.target);
    const file = formData.get('file');
    
    if (file) {
        handleFileUpload(file);
    }
}

// Handle file upload
async function handleFileUpload(file) {
    console.log('handleFileUpload called with file:', file.name);
    
    if (!isAuthenticated) {
        showAlert('Lütfen önce giriş yapın', 'warning');
        return;
    }

    // Validate file type
    const allowedTypes = ['.xlsx', '.xls'];
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
    
    if (!allowedTypes.includes(fileExtension)) {
        showAlert('Sadece Excel dosyaları (.xlsx, .xls) yüklenebilir', 'danger');
        return;
    }

    // Validate file size (50MB)
    const maxSize = 50 * 1024 * 1024; // 50MB
    if (file.size > maxSize) {
        showAlert('Dosya boyutu 50MB\'dan büyük olamaz', 'danger');
        return;
    }

    showLoading();

    try {
        const formData = new FormData();
        formData.append('file', file);
        const descriptionElement = document.getElementById('description');
        formData.append('description', descriptionElement ? descriptionElement.value : '');

        const response = await fetch('/api/excel/upload', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('authToken')}`
            },
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            showAlert('Dosya başarıyla yüklendi!', 'success');
        } else {
            const error = await response.json();
            showAlert(error.message || 'Dosya yüklenirken hata oluştu', 'danger');
        }
    } catch (error) {
        console.error('Upload error:', error);
        showAlert('Dosya yüklenirken hata oluştu', 'danger');
    } finally {
        hideLoading();
    }
}

// Setup file upload
function setupFileUpload() {
    console.log('Setting up file upload...');
    
    const uploadArea = document.querySelector('.upload-area');
    if (!uploadArea) return;

    uploadArea.addEventListener('dragover', function(e) {
        e.preventDefault();
        uploadArea.classList.add('dragover');
    });

    uploadArea.addEventListener('dragleave', function(e) {
        e.preventDefault();
        uploadArea.classList.remove('dragover');
    });

    uploadArea.addEventListener('drop', function(e) {
        e.preventDefault();
        uploadArea.classList.remove('dragover');
        
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            handleFileUpload(files[0]);
        }
    });

    // File input change
    const fileInput = document.querySelector('input[type="file"]');
    if (fileInput) {
        fileInput.addEventListener('change', function(e) {
            if (e.target.files.length > 0) {
                handleFileUpload(e.target.files[0]);
            }
        });
    }
}

// Logout function
function logout() {
    console.log('logout called');
    
    // Clear authentication data
    localStorage.removeItem('authToken');
    isAuthenticated = false;
    currentUser = null;
    
    // Update UI
    setUnauthenticated();
    
    showAlert('Başarıyla çıkış yapıldı', 'success');
    
    // Redirect to login page
    setTimeout(() => {
        window.location.href = '/login';
    }, 1000);
}

// Utility functions
function showAlert(message, type = 'info') {
    console.log('Showing alert:', message, type);
    
    const alertContainer = document.getElementById('alertContainer') || createAlertContainer();
    
    const alert = document.createElement('div');
    alert.className = `alert alert-${type}`;
    alert.innerHTML = `
        ${message}
        <button type="button" class="close" onclick="this.parentElement.remove()">&times;</button>
    `;
    
    alertContainer.appendChild(alert);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (alert.parentElement) {
            alert.remove();
        }
    }, 5000);
}

function createAlertContainer() {
    const container = document.createElement('div');
    container.id = 'alertContainer';
    container.style.position = 'fixed';
    container.style.top = '20px';
    container.style.right = '20px';
    container.style.zIndex = '9999';
    container.style.maxWidth = '400px';
    
    document.body.appendChild(container);
    return container;
}

function showLoading() {
    const loading = document.createElement('div');
    loading.id = 'loading';
    loading.className = 'spinner';
    loading.style.position = 'fixed';
    loading.style.top = '50%';
    loading.style.left = '50%';
    loading.style.transform = 'translate(-50%, -50%)';
    loading.style.zIndex = '9999';
    
    document.body.appendChild(loading);
}

function hideLoading() {
    const loading = document.getElementById('loading');
    if (loading) {
        loading.remove();
    }
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY'
    }).format(amount);
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('tr-TR');
}

// Export functions for global use
window.ExcelUploader = {
    showAlert,
    showLoading,
    hideLoading,
    formatCurrency,
    formatDate,
    logout
};