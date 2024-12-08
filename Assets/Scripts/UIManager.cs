using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject mixedRealityView;
    public GameObject desktopView;

    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX
        desktopView.SetActive(true);
        mixedRealityView.SetActive(false);
        Debug.Log("Active platform: " + Application.platform);
#else
        Debug.Log("Unity Android");
        mixedRealityView.SetActive(true);
        desktopView.SetActive(false);
#endif
    }

    // Update is called once per frame
    void Update()
    {

    }
}
