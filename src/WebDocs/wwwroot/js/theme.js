(function () {
    var STORAGE_KEY = 'drapo-theme';

    function applyTheme(theme) {
        if (theme === 'dark') {
            document.documentElement.classList.add('dark-theme');
        } else {
            document.documentElement.classList.remove('dark-theme');
        }
    }

    function toggleTheme() {
        var isDark = document.documentElement.classList.toggle('dark-theme');
        localStorage.setItem(STORAGE_KEY, isDark ? 'dark' : 'light');
    }

    applyTheme(localStorage.getItem(STORAGE_KEY));

    window.toggleTheme = toggleTheme;
})();
