using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using TMPro;

public class serialtest : MonoBehaviour
{
    [SerializeField]
    TMP_Text textView;
    AndroidJavaClass unityPlayer=null;
    AndroidJavaObject activity=null;

    [SerializeField]
    int maxTextLength = 5000;


    void Start()
    {
        Application.targetFrameRate = 120;
        try
        {
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            if (unityPlayer != null)
            {
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            Debug.Log(activity);
        }
        catch (Exception e)
        {
            Debug.Log("Exception initializing USB:" + e.Message);
        }
    }

    private string read() {
       return activity.Call<String>("read", new object[] {}); 
    }
    private void write(String message) {
        activity.Call("write", new object[] {message});
    }
	
	private bool running = true;
	void OnApplicationQuit()
    {
        running = false;
    }

	void Update()
    {
        write((Time.time + "\n hello").ToString());

        String readVal = read();

        //textView.text += readVal;
        //if (textView.text.Length >= maxTextLength * 1.5) {
            //textView.text = textView.text.Substring(textView.text.Length - maxTextLength, textView.text.Length - (textView.text.Length - maxTextLength));
        //}

        Debug.Log(readVal);
    }
}