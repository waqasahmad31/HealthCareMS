window.healthCareTheme = (function () {
  const key = "HealthCareMS.Theme";

  function getPreferredTheme() {
    const saved = localStorage.getItem(key);
    if (saved === "dark" || saved === "light") {
      return saved;
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }

  function applyTheme(theme) {
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem(key, theme);
    return theme === "dark" ? "Light Mode" : "Dark Mode";
  }

  return {
    init: function () {
      return applyTheme(getPreferredTheme());
    },
    toggle: function () {
      const current = document.documentElement.getAttribute("data-theme") || "light";
      return applyTheme(current === "dark" ? "light" : "dark");
    },
    sync: function () {
      applyTheme(getPreferredTheme());
    }
  };
})();
