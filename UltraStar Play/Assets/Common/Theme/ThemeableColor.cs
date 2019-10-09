﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableColor : Themeable
{
    [ReadOnly]
    public string colorsFileName = "colors";
    [ReadOnly]
    public string colorName;
    public EColorResource colorResource = EColorResource.NONE;
    public Image target;

    private EColorResource lastColorResource = EColorResource.NONE;

    void OnEnable()
    {
        if (target == null)
        {
            target = GetComponent<Image>();
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (lastColorResource != colorResource)
        {
            lastColorResource = colorResource;
            colorsFileName = colorResource.GetPath();
            colorName = colorResource.GetName();
            ReloadResources();
        }
    }
#endif

    public override void ReloadResources()
    {
        if (string.IsNullOrEmpty(colorName))
        {
            Debug.LogWarning($"Missing image name", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        if (TryLoadColorFromTheme(colorsFileName, colorName, out Color loadedColor))
        {
            target.color = loadedColor;
        }
        else
        {
            Debug.LogError($"Could not load color {colorName}", gameObject);
        }
    }
}
