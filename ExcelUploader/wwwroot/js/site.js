// Global variables
let currentUser = null;
let isAuthenticated = false;

// Initialize application
document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
    setupEventListeners();
    checkAuthentication();
});

// Initialize application
function initializeApp() {
    console.log('Excel Uploader Application Initialized');
    
    // Check if user is logged in
    const token = localStorage.getItem('authToken');
    if (token) {
        isAuthenticated = true;
        loadUserProfile();
    }
}

// Setup event listeners
function setupEventListeners() {
    // File upload drag and drop
    setupFileUpload();
    
    // Navigation
    setupNavigation();
    
    // Forms
    setupForms();
    
    // Tables
    setupTables();
}

// File upload functionality
function setupFileUpload() {
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

// Handle file upload
async function handleFileUpload(file) {
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

    // Show loading
    showLoading();

    try {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('description', document.getElementById('description')?.value || '');

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
            
                    // Refresh data if on dashboard
        if (window.location.pathname.includes('index.html') || window.location.pathname.endsWith('/')) {
            loadDashboardData();
        }
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

// Navigation setup
function setupNavigation() {
    const navLinks = document.querySelectorAll('.nav-item a');
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const href = this.getAttribute('href');
            navigateTo(href);
        });
    });
}

// Navigation function
function navigateTo(path) {
    if (path === '/logout') {
        logout();
        return;
    }

    if (path === '/login' || path === '/register') {
        window.location.href = path;
        return;
    }

    // Check authentication for protected routes
    if (path !== '/' && !isAuthenticated) {
        showAlert('Bu sayfaya erişmek için giriş yapmalısınız', 'warning');
        return;
    }

    // Load page content
    loadPageContent(path);
}

// Load page content
async function loadPageContent(path) {
    try {
        const response = await fetch(`/api${path}`);
        if (response.ok) {
            const data = await response.json();
            updatePageContent(data);
        } else {
            showAlert('Sayfa yüklenirken hata oluştu', 'danger');
        }
    } catch (error) {
        console.error('Page load error:', error);
        showAlert('Sayfa yüklenirken hata oluştu', 'danger');
    }
}

// Forms setup
function setupForms() {
    // Login form
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }

    // Register form
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', handleRegister);
    }

    // Upload form
    const uploadForm = document.getElementById('uploadForm');
    if (uploadForm) {
        uploadForm.addEventListener('submit', handleFormUpload);
    }
}

// Handle login
async function handleLogin(e) {
    e.preventDefault();
    
    const formData = new FormData(e.target);
    const email = formData.get('email');
    const password = formData.get('password');
    const rememberMe = formData.get('rememberMe');

    try {
        const response = await fetch('/api/account/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password, rememberMe })
        });

        if (response.ok) {
            const result = await response.json();
            localStorage.setItem('authToken', result.token);
            isAuthenticated = true;
            currentUser = result.user;
            
            showAlert('Başarıyla giriş yapıldı!', 'success');
            setTimeout(() => {
                window.location.href = '/';
            }, 1000);
        } else {
            const error = await response.json();
            showAlert(error.message || 'Giriş başarısız', 'danger');
        }
    } catch (error) {
        console.error('Login error:', error);
        showAlert('Giriş yapılırken hata oluştu', 'danger');
    }
}

// Handle register
async function handleRegister(e) {
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
    e.preventDefault();
    
    const formData = new FormData(e.target);
    const file = formData.get('file');
    
    if (file) {
        handleFileUpload(file);
    }
}

// Tables setup
function setupTables() {
    // Add sorting functionality
    const tableHeaders = document.querySelectorAll('.table th[data-sort]');
    tableHeaders.forEach(header => {
        header.addEventListener('click', function() {
            const column = this.getAttribute('data-sort');
            const currentOrder = this.getAttribute('data-order') || 'asc';
            const newOrder = currentOrder === 'asc' ? 'desc' : 'asc';
            
            // Update all headers
            tableHeaders.forEach(h => h.setAttribute('data-order', ''));
            this.setAttribute('data-order', newOrder);
            
            // Sort table
            sortTable(column, newOrder);
        });
    });

    // Add search functionality
    const searchInput = document.querySelector('.table-search');
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            const searchTerm = this.value.toLowerCase();
            filterTable(searchTerm);
        });
    }
}

// Sort table
function sortTable(column, order) {
    const table = document.querySelector('.table');
    const tbody = table.querySelector('tbody');
    const rows = Array.from(tbody.querySelectorAll('tr'));

    rows.sort((a, b) => {
        const aValue = a.querySelector(`td[data-${column}]`)?.textContent || '';
        const bValue = b.querySelector(`td[data-${column}]`)?.textContent || '';

        if (order === 'asc') {
            return aValue.localeCompare(bValue);
        } else {
            return bValue.localeCompare(aValue);
        }
    });

    // Reorder rows
    rows.forEach(row => tbody.appendChild(row));
}

// Filter table
function filterTable(searchTerm) {
    const table = document.querySelector('.table');
    const rows = table.querySelectorAll('tbody tr');

    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        if (text.includes(searchTerm)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

// Load dashboard data
async function loadDashboardData() {
    try {
        const response = await fetch('/api/home/dashboard');
        if (response.ok) {
            const data = await response.json();
            updateDashboard(data);
        }
    } catch (error) {
        console.error('Dashboard load error:', error);
    }
}

// Update dashboard
function updateDashboard(data) {
    // Update stats
    if (data.totalRecords !== undefined) {
        document.getElementById('totalRecords')?.textContent = data.totalRecords;
    }
    if (data.processedRecords !== undefined) {
        document.getElementById('processedRecords')?.textContent = data.processedRecords;
    }
    if (data.pendingRecords !== undefined) {
        document.getElementById('pendingRecords')?.textContent = data.pendingRecords;
    }
    if (data.totalGrantAmount !== undefined) {
        document.getElementById('totalGrantAmount')?.textContent = formatCurrency(data.totalGrantAmount);
    }
    if (data.totalPaidAmount !== undefined) {
        document.getElementById('totalPaidAmount')?.textContent = formatCurrency(data.totalPaidAmount);
    }

    // Update recent uploads
    if (data.recentUploads) {
        updateRecentUploads(data.recentUploads);
    }

    // Update dynamic tables
    if (data.dynamicTables) {
        updateDynamicTables(data.dynamicTables);
    }
}

// Update recent uploads
function updateRecentUploads(uploads) {
    const container = document.getElementById('recentUploads');
    if (!container) return;

    container.innerHTML = uploads.map(upload => `
        <tr>
            <td>${upload.fileName}</td>
            <td>${upload.uploadDate}</td>
            <td>${upload.recordCount}</td>
            <td>${upload.status}</td>
        </tr>
    `).join('');
}

// Update dynamic tables
function updateDynamicTables(tables) {
    const container = document.getElementById('dynamicTables');
    if (!container) return;

    container.innerHTML = tables.map(table => `
        <div class="stat-card">
            <div class="stat-number">${table.recordCount}</div>
            <div class="stat-label">${table.tableName}</div>
        </div>
    `).join('');
}

// Load user profile
async function loadUserProfile() {
    try {
        const response = await fetch('/api/account/profile', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('authToken')}`
            }
        });

        if (response.ok) {
            currentUser = await response.json();
            updateUserInterface();
        }
    } catch (error) {
        console.error('Profile load error:', error);
    }
}

// Update user interface
function updateUserInterface() {
    if (currentUser) {
        const userNameElement = document.getElementById('userName');
        if (userNameElement) {
            userNameElement.textContent = `${currentUser.firstName} ${currentUser.lastName}`;
        }

        // Show authenticated elements
        const authElements = document.querySelectorAll('.auth-only');
        authElements.forEach(el => el.style.display = '');

        // Hide unauthenticated elements
        const unauthElements = document.querySelectorAll('.unauth-only');
        unauthElements.forEach(el => el.style.display = 'none');
    }
}

// Check authentication
function checkAuthentication() {
    const token = localStorage.getItem('authToken');
    if (token) {
        isAuthenticated = true;
        loadUserProfile();
    } else {
        // Show unauthenticated elements
        const unauthElements = document.querySelectorAll('.unauth-only');
        unauthElements.forEach(el => el.style.display = '');
        
        // Hide authenticated elements
        const authElements = document.querySelectorAll('.auth-only');
        authElements.forEach(el => el.style.display = 'none');
    }
}

// Logout
function logout() {
    localStorage.removeItem('authToken');
    isAuthenticated = false;
    currentUser = null;
    
    showAlert('Başarıyla çıkış yapıldı', 'success');
    setTimeout(() => {
                    window.location.href = '/login';
    }, 1000);
}

// Utility functions
function showAlert(message, type = 'info') {
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

// Navigation function
function navigateTo(page) {
    window.location.href = page;
}

// Dashboard functions
async function loadDashboardData() {
    try {
        const response = await fetch('/api/dashboard/stats', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('authToken')}`
            }
        });

        if (response.ok) {
            const stats = await response.json();
            updateDashboardStats(stats);
        }
    } catch (error) {
        console.error('Dashboard data load error:', error);
    }
}

function updateDashboardStats(stats) {
    if (stats.totalRecords !== undefined) {
        document.getElementById('totalRecords').textContent = stats.totalRecords;
    }
    if (stats.processedRecords !== undefined) {
        document.getElementById('processedRecords').textContent = stats.processedRecords;
    }
    if (stats.pendingRecords !== undefined) {
        document.getElementById('pendingRecords').textContent = stats.pendingRecords;
    }
    if (stats.totalGrantAmount !== undefined) {
        document.getElementById('totalGrantAmount').textContent = formatCurrency(stats.totalGrantAmount);
    }
    if (stats.totalPaidAmount !== undefined) {
        document.getElementById('totalPaidAmount').textContent = formatCurrency(stats.totalPaidAmount);
    }
}

// Setup navigation
function setupNavigation() {
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            if (this.getAttribute('href').startsWith('#')) {
                e.preventDefault();
                const targetId = this.getAttribute('href').substring(1);
                const targetElement = document.getElementById(targetId);
                if (targetElement) {
                    targetElement.scrollIntoView({ behavior: 'smooth' });
                }
            }
        });
    });
}

// Setup forms
function setupForms() {
    // Login form
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }

    // Register form
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', handleRegister);
    }

    // Upload form
    const uploadForm = document.getElementById('uploadForm');
    if (uploadForm) {
        uploadForm.addEventListener('submit', handleUploadForm);
    }
}

// Setup tables
function setupTables() {
    // Add any table-specific event listeners here
}

// Handle login
async function handleLogin(e) {
    e.preventDefault();
    
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const rememberMe = document.getElementById('rememberMe')?.checked || false;

    try {
        showLoading();
        
        const response = await fetch('/api/account/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email,
                password,
                rememberMe
            })
        });

        if (response.ok) {
            const result = await response.json();
            localStorage.setItem('authToken', result.token);
            isAuthenticated = true;
            currentUser = result.user;
            
            showAlert('Başarıyla giriş yapıldı', 'success');
            setTimeout(() => {
                window.location.href = '/';
            }, 1000);
        } else {
            const error = await response.json();
            showAlert(error.message || 'Giriş başarısız', 'danger');
        }
    } catch (error) {
        console.error('Login error:', error);
        showAlert('Giriş başarısız', 'danger');
    } finally {
        hideLoading();
    }
}

// Handle register
async function handleRegister(e) {
    e.preventDefault();
    
    const firstName = document.getElementById('firstName').value;
    const lastName = document.getElementById('lastName').value;
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;

    if (password !== confirmPassword) {
        showAlert('Şifreler eşleşmiyor', 'danger');
    }

    try {
        showLoading();
        
        const response = await fetch('/api/account/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                firstName,
                lastName,
                email,
                password
            })
        });

        if (response.ok) {
            showAlert('Kayıt başarılı! Giriş yapabilirsiniz.', 'success');
            setTimeout(() => {
                window.location.href = '/login';
            }, 2000);
        } else {
            const error = await response.json();
            showAlert(error.message || 'Kayıt başarısız', 'danger');
        }
    } catch (error) {
        console.error('Register error:', error);
        showAlert('Kayıt yapılırken hata oluştu', 'danger');
    } finally {
        hideLoading();
    }
}

// Handle upload form
async function handleUploadForm(e) {
    e.preventDefault();
    
    const fileInput = document.getElementById('file');
    const description = document.getElementById('description')?.value || '';
    
    if (!fileInput.files[0]) {
        showAlert('Lütfen bir dosya seçin', 'warning');
        return;
    }

    await handleFileUpload(fileInput.files[0], description);
}

// Load user profile
async function loadUserProfile() {
    try {
        const response = await fetch('/api/account/profile', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('authToken')}`
            }
        });

        if (response.ok) {
            currentUser = await response.json();
            updateUserInterface();
        }
    } catch (error) {
        console.error('Profile load error:', error);
    }
}

// Update user interface
function updateUserInterface() {
    if (isAuthenticated && currentUser) {
        // Show authenticated elements
        const authElements = document.querySelectorAll('.auth-only');
        authElements.forEach(el => el.style.display = '');
        
        // Hide unauthenticated elements
        const unauthElements = document.querySelectorAll('.unauth-only');
        unauthElements.forEach(el => el.style.display = 'none');
        
        // Update user name
        const userNameElements = document.querySelectorAll('#userName');
        userNameElements.forEach(el => {
            el.textContent = currentUser.firstName || 'Profil';
        });
    } else {
        // Show unauthenticated elements
        const unauthElements = document.querySelectorAll('.unauth-only');
        unauthElements.forEach(el => el.style.display = '');
        
        // Hide authenticated elements
        const authElements = document.querySelectorAll('.auth-only');
        authElements.forEach(el => el.style.display = 'none');
    }
}

// Show message function for tables page
function showMessage(message, type) {
    if (window.ExcelUploader && window.ExcelUploader.showAlert) {
        window.ExcelUploader.showAlert(message, type);
    } else {
        // Fallback alert
        alert(`${type}: ${message}`);
    }
}

// Export functions for global use
window.ExcelUploader = {
    showAlert,
    showLoading,
    hideLoading,
    formatCurrency,
    formatDate,
    logout,
    navigateTo,
    showMessage
};