using UnityEditor;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    /// <summary>
    /// Lets an author point the editor tooling at another Application API — one running on this
    /// machine, or a different environment — to test changes that are not deployed yet.
    /// See <see cref="EditorApiEndpoint"/>.
    ///
    /// Every edit saves immediately. An earlier version had an Apply button, and the obvious
    /// failure mode showed up on first use: the URL was typed, Apply was not pressed, the
    /// tooling kept calling the tenant, and the only symptom was a 404 from a route that does
    /// not exist there yet. A settings window with an unsaved state is a trap.
    /// </summary>
    public class EditorApiEndpointWindow : EditorWindow
    {
        private string url;

        [MenuItem("Reflectis Worlds/Creator Kit/Development/API endpoint...")]
        public static void ShowWindow()
        {
            EditorApiEndpointWindow window = GetWindow<EditorApiEndpointWindow>(true, "API endpoint", true);
            window.url = EditorApiEndpoint.Override;
            window.minSize = new Vector2(480, 200);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Overrides the Application API that the Creator Kit tooling calls: addressables " +
                "deploy, interpreted-script verification and environment DLL import all follow it. " +
                "Leave empty to use the logged-in tenant's API. Changes are saved as you type.",
                MessageType.None);

            EditorGUILayout.Space();

            string tenantUrl = EditorLoginState.CurrentTenant?.Config?.ApplicationApiUrl;
            EditorGUILayout.LabelField("Tenant API", string.IsNullOrEmpty(tenantUrl) ? "(not logged in)" : tenantUrl);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            url = EditorGUILayout.TextField("Override", url);
            if (EditorGUI.EndChangeCheck())
            {
                Save(url);
            }

            // No preset button: a hardcoded URL here is how one developer's machine ends up
            // baked into a shared package. Type the address once, it persists.
            if (GUILayout.Button("Use tenant API"))
            {
                url = string.Empty;
                Save(url);
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space();

            // What the tooling will actually call, read back from where it is stored rather than
            // from the field: the point of this box is to prove the setting took effect.
            string effective = EditorApiEndpoint.ApplicationApiUrl;

            if (EditorApiEndpoint.IsOverridden)
            {
                EditorGUILayout.HelpBox($"Tooling is calling {effective}, NOT the tenant.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(effective)
                        ? "No override and no tenant logged in: the tooling has no API to call."
                        : $"Tooling is calling the tenant API: {effective}",
                    MessageType.Info);
            }
        }

        private static void Save(string value)
        {
            EditorApiEndpoint.Override = value;
            Debug.Log($"[ApiEndpoint] Creator Kit tooling now calls: " +
                      $"{EditorApiEndpoint.ApplicationApiUrl ?? "(no tenant logged in)"}");
        }
    }
}
