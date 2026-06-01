
## 2026-06-01: Slider alignment fix

- `FlexibleSpace` without explicit height consumes all remaining vertical space, defeating `TextAnchor.MiddleLeft` on labels.
- Fix: remove `BeginVertical`/`FlexibleSpace`/`EndVertical` wrappers; give both label and slider matching `GUILayout.Height(20)`.
- Matching heights on label+slider ensures they share the same line height regardless of surrounding layout.
- Used explicit Width values: labels 145/70, sliders 160/140.
