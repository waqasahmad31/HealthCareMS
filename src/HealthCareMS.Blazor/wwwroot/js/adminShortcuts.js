window.healthCareAdminShortcuts = (function () {
  let handler = null;
  let dotNetRef = null;

  function onKeyDown(event) {
    const isSave = (event.ctrlKey || event.metaKey) && event.key && event.key.toLowerCase() === "s";
    if (!isSave || !dotNetRef) {
      return;
    }

    event.preventDefault();
    dotNetRef.invokeMethodAsync("HandleSaveShortcutAsync");
  }

  return {
    register: function (reference) {
      dotNetRef = reference;
      if (!handler) {
        handler = onKeyDown;
        window.addEventListener("keydown", handler);
      }
    },
    unregister: function () {
      if (handler) {
        window.removeEventListener("keydown", handler);
        handler = null;
      }

      dotNetRef = null;
    }
  };
})();
