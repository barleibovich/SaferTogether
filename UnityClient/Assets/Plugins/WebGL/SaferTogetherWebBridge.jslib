mergeInto(LibraryManager.library, {
  SaferTogetherNavigate: function (urlPointer) {
    window.location.href = UTF8ToString(urlPointer);
  }
});
