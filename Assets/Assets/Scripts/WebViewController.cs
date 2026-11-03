/*
- Copyright (C) 2012 GREE, Inc.
- 
- This software is provided 'as-is', without any express or implied
- warranty.  In no event will the authors be held liable for any damages
- arising from the use of this software.
- 
- Permission is granted to anyone to use this software for any purpose,
- including commercial applications, and to alter it and redistribute it
- freely, subject to the following restrictions:
- 
- 1. The origin of this software must not be misrepresented; you must not
-    claim that you wrote the original software. If you use this software
-    in a product, an acknowledgment in the product documentation would be
-    appreciated but is not required.
- 2. Altered source versions must be plainly marked as such, and must not be
-    misrepresented as being the original software.
- 3. This notice may not be removed or altered from any source distribution.
 */

using Gree.UnityWebView;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WebViewController : MonoBehaviour
{
    public string Url;
    public int LeftMargin, RightMargin, TopMargin, BottomMargin;

    [SerializeField]
    private WebViewObject webViewObject;

    private Coroutine _loadCoroutine;

    private void Start()
    {
        _loadCoroutine = StartCoroutine(LoadWebView(Url));
        SetVisibility(true);
    }

    private void OnDisable()
    {
        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
        }
    }

    public void SetVisibility(bool visibility)
    {
        webViewObject.SetVisibility(visibility);
    }

    public bool GetVisibility()
    {
        return webViewObject.GetVisibility();
    }

    private IEnumerator LoadWebView(string Url)
    {
        webViewObject.Init(
            cb: (msg) =>
            {
                // Debug.Log(string.Format("CallFromJS[{0}]", msg));
            },
            err: (msg) =>
            {
                // Debug.Log(string.Format("CallOnError[{0}]", msg));
            },
            httpErr: (msg) =>
            {
                // Debug.Log(string.Format("CallOnHttpError[{0}]", msg));
            },
            started: (msg) =>
            {
                // Debug.Log(string.Format("CallOnStarted[{0}]", msg));
            },
            hooked: (msg) =>
            {
                // Debug.Log(string.Format("CallOnHooked[{0}]", msg));
            },
            cookies: (msg) =>
            {
                // Debug.Log(string.Format("CallOnCookies[{0}]", msg));
            },
            ld: (msg) =>
            {
                // Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
#if true
                var js = @"
                    if (!(window.webkit && window.webkit.messageHandlers)) {
                        window.Unity = {
                            call: function(msg) {
                                window.location = 'unity:' + msg;
                            }
                        };
                    }
                ";
#else
                var js = @"
                    if (!(window.webkit && window.webkit.messageHandlers)) {
                        window.Unity = {
                            call: function(msg) {
                                var iframe = document.createElement('IFRAME');
                                iframe.setAttribute('src', 'unity:' + msg);
                                document.documentElement.appendChild(iframe);
                                iframe.parentNode.removeChild(iframe);
                                iframe = null;
                            }
                        };
                    }
                ";
#endif
#elif UNITY_WEBPLAYER || UNITY_WEBGL
                var js = @"
                    window.Unity = {
                        call:function(msg) {
                            parent.unityWebView.sendMessage('WebViewObject', msg);
                        }
                    };
                ";
#else
                var js = "";
#endif
                webViewObject.EvaluateJS(js + @"Unity.call('ua=' + navigator.userAgent)");
            }
        );

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        webViewObject.bitmapRefreshCycle = 1;
        webViewObject.devicePixelRatio = 1;
#endif

        webViewObject.SetMargins(LeftMargin, TopMargin, RightMargin, BottomMargin);
        webViewObject.SetTextZoom(100);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
        if (Url.StartsWith("http"))
        {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        }
        else
        {
            var exts = new string[]{
                ".jpg",
                ".js",
                ".html"
            };
            foreach (var ext in exts)
            {
                var url = Url.Replace(".html", ext);
                var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
                var dst = System.IO.Path.Combine(Application.temporaryCachePath, url);
                byte[] result = null;
                if (src.Contains("://"))
                {
#if UNITY_2018_4_OR_NEWER
                    var unityWebRequest = UnityWebRequest.Get(src);
                    yield return unityWebRequest.SendWebRequest();
                    result = unityWebRequest.downloadHandler.data;
#else
                    var www = new WWW(src);
                    yield return www;
                    result = www.bytes;
#endif
                }
                else
                {
                    result = System.IO.File.ReadAllBytes(src);
                }
                System.IO.File.WriteAllBytes(dst, result);
                if (ext == ".html")
                {
                    webViewObject.LoadURL("file://" + dst.Replace(" ", "%20"));
                    break;
                }
            }
        }
#else
        if (Url.StartsWith("http")) {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        } else {
            webViewObject.LoadURL("StreamingAssets/" + Url.Replace(" ", "%20"));
        }
#endif
        yield break;
    }
}