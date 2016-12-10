﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using UnityEngine.UI;

public class NotificationManager : Singleton<NotificationManager> {

    public delegate void NewNotification(Notification notification, Color color);
    public static event NewNotification newNotification;

    public delegate void NewMenu(Menu menu);
    public static event NewMenu newMenu;

    public delegate void NotificationCanceled();
    public static event NotificationCanceled notificationCanceled;

    // Time notification should be displayed. Void if notification requires user action
    private static float TimeTillExpiration = 6f;

    private static Canvas canvas;
    private static Color color;

    private static GameObject panelBorderGO;

    private static NotificationManager notificationManager;

    private static Coroutine coroutine;
    private static bool notificationActive;
    private static bool menuActive;

    private GameObject hueBridgeGO;
    private HueBridgeManager hueBridgeManager;

    void Awake()
    {
        Debug.Log("NotifMgr Awake");
        notificationManager = this;
    }

    void Start () {
        Debug.Log("NotifMgr Start");

        // TODO search through children for name. Don't rely on Canvas being first 
        foreach (Transform child in transform)
        {
            if (child.name == "Canvas")
            {
                GameObject canvasGO = child.gameObject;
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.enabled = false;

                foreach (Transform grandchild in canvasGO.transform)
                {
                    if (grandchild.name == "PanelBorder")
                    {
                         panelBorderGO = grandchild.gameObject;
                    }
                }
            }
        }
        
        if (!canvas)
        {
            Debug.Log("No child Canvas was found. Please add one to use notification system.");
        }

        // TODO create a more reliable solution - used to prevent null object reference
        hueBridgeGO = GameObject.Find("AppManager");
        hueBridgeManager = hueBridgeGO.GetComponent<HueBridgeManager>();
        hueBridgeManager.InitHueBridgeManager();
    }
	
    public static void DisplayNotification(Notification notification)
    {
        if (newNotification != null)
        {
            canvas.enabled = true;

            if (notification.Type == "error")
            {
                color = Color.red;
                SoundManager.instance.PlayNotificationPopup("beepup");

            } else if (notification.Type == "alert")
            {
                // color = steelblue
                color = new Color(0.27f, 0.5f, 0.7f);
                SoundManager.instance.PlayNotificationPopup("tonebeep");
            }

            if (notification.SendToConsole)
            {
                Debug.Log(notification.Message);
            }

            newNotification(notification, color);

            if (!notification.RequiresAction)
            {
                // if notification is active, we discard the previous expiration timer before we start a new one
                if (notificationActive)
                {
                    notificationManager.StopCoroutine(coroutine);
                }
                notificationActive = true;

                var expiration = TimeTillExpiration;
                if (notification.Expiration != 0)
                {
                    expiration = notification.Expiration;
                }
                coroutine = notificationManager.StartCoroutine(NotificationExpiration(expiration));
            }
            else
            {
                // TODO handle notifications that require a user action
            }
        }
    }

    public static void CancelNotification()
    {
        canvas.enabled = false;
        notificationActive = false;

        if (notificationCanceled != null)
        {
            notificationCanceled();
        }
    }

    public static void DisplayMenu(Menu menu)
    {
        if (newMenu != null)
        {
            canvas.enabled = true;

            RectTransform rt = panelBorderGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(menu.Width, menu.Height);

            SoundManager.instance.PlayNotificationPopup("beepup");
            newMenu(menu);
        }

        if (!menu.RequiresAction)
        {
            // if menu is active, we discard the previous expiration timer before we start a new one
            if (menuActive)
            {
                notificationManager.StopCoroutine(coroutine);
            }
            menuActive = true;

            var expiration = TimeTillExpiration;
            if (menu.Expiration != 0)
            {
                expiration = menu.Expiration;
            }
            coroutine = notificationManager.StartCoroutine(NotificationExpiration(expiration));
        }
    }

    private static IEnumerator NotificationExpiration(float seconds)
    {    
        yield return new WaitForSeconds(seconds);
        CancelNotification();
    }
}
