using UnityEngine;
using UnityEngine.UI;

namespace nioruka.API_and_JSON
{
    public class ApiButtonBinder : MonoBehaviour
    {
        public Button btnGet;
        public Button btnPost;
        public ApiCallerRawJson api;

        void Awake()
        {
            if (btnGet != null && api != null)
                btnGet.onClick.AddListener(api.OnClick_GetRawJson);

            if (btnPost != null && api != null)
                btnPost.onClick.AddListener(api.OnClick_PostJson);
        }
    }
}