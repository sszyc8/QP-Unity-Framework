﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusButton : MonoBehaviour {
//	public string[] Res = {"",""};
	public string[] dataKeys;
    public string dataValueKey;
    public Sprite[] Sprites;
	public string[] Texts;
    int status = 0;
	Image image;
	Text text;
	void Start() {
		image = transform.GetComponent<Image>();
		text = transform.GetComponentInChildren<Text>();
        DataBase data = DataManager.Instance.getData(dataKeys);
        data.Bind(setSatus);
        LocalizationManager.Instance.Bind(ChangeText);
    }
    void ChangeText(DataBase data){
        text.text = LocalizationManager.Instance.GetLocalizedValue(Texts[status]);
    }
	public void setSatus(DataBase data){
        status = data.GetIntValue(dataValueKey);
        if (image && Sprites.Length > status)
        {
            image.sprite = Sprites[status];
        }
        if (text && Texts.Length > status)
        {
            text.text = LocalizationManager.Instance.GetLocalizedValue(Texts[status]);
        }
    }
}
