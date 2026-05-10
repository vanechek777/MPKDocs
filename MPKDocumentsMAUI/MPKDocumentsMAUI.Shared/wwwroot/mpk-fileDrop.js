export function initFileDrop(dotNetRef, element) {
  const ac = new AbortController();
  const { signal } = ac;
  // capture: true — dragover с дочерних узлов (текст, кнопка) отрабатывает до target,
  // иначе без preventDefault на родителе курсор остаётся «запрет» (WinUI/WebView2).
  const cap = { signal, capture: true };
  element.addEventListener(
    "dragover",
    (e) => {
      e.preventDefault();
      e.dataTransfer.dropEffect = "copy";
    },
    cap
  );
  element.addEventListener(
    "drop",
    async (e) => {
      e.preventDefault();
      e.stopPropagation();
      const f = e.dataTransfer?.files?.[0];
      if (!f) return;
      const name = f.name;
      const l = name.toLowerCase();
      if (!l.endsWith(".pdf") && !l.endsWith(".doc") && !l.endsWith(".docx")) return;
      await dotNetRef.invokeMethodAsync("NotifyDroppedFile", name, f.size);
    },
    cap
  );
  return {
    dispose: () => ac.abort(),
  };
}
