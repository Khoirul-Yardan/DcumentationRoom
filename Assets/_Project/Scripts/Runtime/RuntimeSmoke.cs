using System.Collections;
using System.IO;
using ProjectAtlas.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectAtlas.Runtime
{
    // Opt-in standalone smoke route; never runs during ordinary Play.
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var app = GetComponent<AppBootstrapper>();
            string output = Path.Combine(System.Environment.CurrentDirectory, "Docs", "Evidence"); Directory.CreateDirectory(output);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "01-menu.png"));
            app.Navigate("Lobby"); yield return new WaitUntil(() => !app.IsLoading); yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "02-lobby.png"));
            app.Navigate("Gallery"); yield return new WaitUntil(() => !app.IsLoading); yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "03-gallery.png"));
            app.OpenProject(app.Catalog.projects[0].id); yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "04-detail.png"));
            app.ui.Viewer(app.Catalog.projects[0].coverMediaId); yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "05-viewer.png"));
            bool image = FindFirstObjectByType<ImageViewer>()?.image.texture != null;
            app.ui.Back(); bool detail = app.ui.HasModal && !app.player.Active;
            app.ui.Back();
            app.Navigate("Lobby"); yield return new WaitUntil(() => !app.IsLoading);
            bool singleton = FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1;
            File.WriteAllText(Path.Combine(output, "runtime-smoke.txt"), $"Image loaded: {image}\nModal restores detail and blocks movement: {detail}\nSingle player/EventSystem: {singleton}\nReturn zone: {app.Zone}\n");
            yield return new WaitForSeconds(1);
            UnityEngine.Application.Quit(image && detail && singleton && app.Zone == "Lobby" ? 0 : 1);
        }
    }
}
