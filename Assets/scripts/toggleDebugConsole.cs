using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IngameDebugConsole;

public class toggleDebugConsole : MonoBehaviour
{
    [SerializeField]
    private DebugLogManager debugMenu;
    [SerializeField]
    private Toggle toggle;

    public void updateActive() {
        //debugMenu.SetActive(toggle.isOn);
        debugMenu.PopupEnabled = toggle.isOn;
    }

}
