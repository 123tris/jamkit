// WebGL helpers for JamKit. persistentDataPath on WebGL is an in-memory FS backed by IndexedDB;
// without an explicit syncfs, saves written with File.WriteAllText can vanish when the tab closes.
mergeInto(LibraryManager.library, {
  JamKitSyncFiles: function () {
    if (typeof FS !== 'undefined' && typeof FS.syncfs === 'function') {
      FS.syncfs(false, function (err) {
        if (err) console.warn('[JamKit] FS.syncfs failed:', err);
      });
    }
  }
});
