// using UnityEngine;
// using UnityEditor;
// using TMPro;
//
// [InitializeOnLoad]
// public static class MigrateYarnUI {
//     static MigrateYarnUI() {
//         EditorApplication.delayCall += Migrate;
//     }
//     public static void Migrate() {
//         if (EditorPrefs.GetBool("YarnMigrated", false)) return;
//         EditorPrefs.SetBool("YarnMigrated", true);
//
//         var oldDia = GameObject.Find("Dialogue");
//         if (oldDia == null) return;
//         var yarnSys = GameObject.Find("Dialogue System");
//         if (yarnSys == null) return;
//         var yarnCanvas = yarnSys.transform.Find("Canvas");
//         if (yarnCanvas == null) return;
//
//         DebugEditor.Log("Found old Dialogue and Yarn Canvas! Running UI layout sync.");
//         var oldRect = oldDia.GetComponent<RectTransform>();
//         var linePresenter = yarnCanvas.transform.Find("Line Presenter");
//         if (linePresenter != null) {
//             CopyRect(oldRect, linePresenter.GetComponent<RectTransform>());
//         }
//         var background = linePresenter.transform.Find("Background");
//         if (background != null && background.TryGetComponent<UnityEngine.UI.Image>(out var bgImg)) {
//             var oldImg = oldDia.GetComponent<UnityEngine.UI.Image>();
//             if (oldImg != null) {
//                 bgImg.sprite = oldImg.sprite;
//                 bgImg.color = oldImg.color;
//                 CopyRect(oldImg.rectTransform, bgImg.rectTransform);
//             }
//         }
//         
//         var oldSpeaker = GetChild(oldDia.transform, "speakerText");
//         var oldContent = GetChild(oldDia.transform, "contentText");
//         var yarnChar = yarnCanvas.transform.Find("Line Presenter/Character Name");
//         var yarnText = yarnCanvas.transform.Find("Line Presenter/Text");
//         
//         if (oldSpeaker != null && yarnChar != null) {
//             CopyRect(oldSpeaker.GetComponent<RectTransform>(), yarnChar.GetComponent<RectTransform>());
//             CopyTMP(oldSpeaker.GetComponent<TextMeshProUGUI>(), yarnChar.GetComponent<TextMeshProUGUI>());
//         }
//         if (oldContent != null && yarnText != null) {
//             CopyRect(oldContent.GetComponent<RectTransform>(), yarnText.GetComponent<RectTransform>());
//             CopyTMP(oldContent.GetComponent<TextMeshProUGUI>(), yarnText.GetComponent<TextMeshProUGUI>());
//         }
//         
//         oldDia.SetActive(false);
//         var uiDia = oldDia.GetComponent("UIDialogue");
//         if (uiDia != null) Object.DestroyImmediate(uiDia);
//
//         UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
//     }
//
//     static Transform GetChild(Transform parent, string name) {
//         foreach (Transform t in parent.GetComponentsInChildren<Transform>(true)) {
//             if (t.name.Contains(name)) return t;
//         }
//         return null;
//     }
//
//     static void CopyRect(RectTransform src, RectTransform dst) {
//         if (src == null || dst == null) return;
//         dst.anchorMin = src.anchorMin; dst.anchorMax = src.anchorMax;
//         dst.anchoredPosition = src.anchoredPosition; dst.sizeDelta = src.sizeDelta;
//         dst.pivot = src.pivot; dst.offsetMin = src.offsetMin; dst.offsetMax = src.offsetMax;
//     }
//     static void CopyTMP(TextMeshProUGUI src, TextMeshProUGUI dst) {
//         if (src == null || dst == null) return;
//         dst.font = src.font; dst.fontSize = src.fontSize;
//         dst.fontSizeMin = src.fontSizeMin; dst.fontSizeMax = src.fontSizeMax;
//         dst.enableAutoSizing = src.enableAutoSizing; dst.color = src.color;
//         dst.alignment = src.alignment; dst.lineSpacing = src.lineSpacing; dst.characterSpacing = src.characterSpacing;
//     }
// }

