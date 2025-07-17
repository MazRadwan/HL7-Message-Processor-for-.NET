// Custom JavaScript for HL7 Processor

// Initialize SignalR connection for real-time updates
let connection = null;

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    initializeSignalR();
    initializeSidebar();
});

// SignalR initialization
function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/systemhub")
        .build();

    connection.start().then(function () {
        console.log('SignalR Connected');
    }).catch(function (err) {
        console.error('SignalR Connection Error: ', err.toString());
        // Retry connection every 5 seconds
        setTimeout(initializeSignalR, 5000);
    });

    // Handle connection errors
    connection.onclose(function (error) {
        console.log('SignalR Connection Closed');
        // Attempt to reconnect
        setTimeout(initializeSignalR, 5000);
    });

    // Listen for system health updates
    connection.on("SystemHealthUpdate", function (health) {
        updateSystemHealthIndicator(health);
    });
}

// Update system health indicator in the UI
function updateSystemHealthIndicator(health) {
    // This would be called from Blazor components
    console.log('System Health Update:', health);
}

// Sidebar functionality
function initializeSidebar() {
    // Auto-collapse sidebar on mobile
    function handleResize() {
        const wrapper = document.getElementById('wrapper');
        if (window.innerWidth < 768) {
            wrapper.classList.add('toggled');
        } else {
            wrapper.classList.remove('toggled');
        }
    }

    // Initial check
    handleResize();

    // Listen for resize events
    window.addEventListener('resize', handleResize);
}

// Toggle sidebar
function toggleSidebar() {
    document.getElementById('wrapper').classList.toggle('toggled');
}

// Utility functions for charts and UI interactions
window.chartUtils = {
    // Helper function to format dates for charts
    formatTimeLabel: function(date) {
        return date.toLocaleTimeString('en-US', { 
            hour: '2-digit', 
            minute: '2-digit',
            hour12: false 
        });
    },

    // Generate color palette for charts
    generateColors: function(count, alpha = 1) {
        const colors = [
            `rgba(75, 192, 192, ${alpha})`,
            `rgba(255, 99, 132, ${alpha})`,
            `rgba(54, 162, 235, ${alpha})`,
            `rgba(255, 205, 86, ${alpha})`,
            `rgba(153, 102, 255, ${alpha})`,
            `rgba(255, 159, 64, ${alpha})`
        ];
        
        const result = [];
        for (let i = 0; i < count; i++) {
            result.push(colors[i % colors.length]);
        }
        return result;
    }
};

// Toast notifications
window.showToast = function(message, type = 'info') {
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    // Add to toast container (create if doesn't exist)
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1200';
        document.body.appendChild(container);
    }

    container.appendChild(toast);

    // Initialize and show toast
    const bsToast = new bootstrap.Toast(toast, {
        autohide: true,
        delay: 5000
    });
    bsToast.show();

    // Remove from DOM after hiding
    toast.addEventListener('hidden.bs.toast', function() {
        container.removeChild(toast);
    });
};

// Loading states
window.setLoading = function(elementId, isLoading) {
    const element = document.getElementById(elementId);
    if (element) {
        if (isLoading) {
            element.classList.add('loading');
        } else {
            element.classList.remove('loading');
        }
    }
};

// Local storage helpers
window.localStorageHelper = {
    get: function(key) {
        try {
            return localStorage.getItem(key);
        } catch {
            return null;
        }
    },
    
    set: function(key, value) {
        try {
            localStorage.setItem(key, value);
            return true;
        } catch {
            return false;
        }
    },
    
    remove: function(key) {
        try {
            localStorage.removeItem(key);
            return true;
        } catch {
            return false;
        }
    }
};