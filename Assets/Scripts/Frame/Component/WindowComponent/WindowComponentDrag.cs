﻿using UnityEngine;
using System;
using System.Collections;

public class WindowComponentDrag : ComponentDrag
{
	public WindowComponentDrag(Type type, string name)
		:
		base(type, name)
	{}
	public override void update(float elapsedTime)
	{
		base.update(elapsedTime);
	}
	//--------------------------------------------------------------------------------------------------------------
	protected override bool isType(Type type) { return base.isType(type) || type == typeof(WindowComponentDrag); }
	protected override void applyScreenPosition(Vector3 screenPos)
	{
		txUIObject window = mComponentOwner as txUIObject;
		window.setLocalPosition(UnityUtility.screenPosToWindowPos(screenPos, window.getParent()) - 
			new Vector2(Screen.currentResolution.width / 2.0f, Screen.currentResolution.height / 2.0f));
	}
	protected override Vector3 getScreenPosition()
	{
		return UnityUtility.worldPosToScreenPos((mComponentOwner as txUIObject).getWorldPosition());
	}
	protected override bool mouseHovered(Vector3 mousePosition)
	{
		txUIObject obj = mComponentOwner as txUIObject;
		return obj.getMouseHovered();
	}
}