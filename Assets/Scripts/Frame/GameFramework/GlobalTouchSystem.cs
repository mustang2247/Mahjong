﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ColliderCallBack
{
	public txUIObject mButton;
	public BoxColliderClickCallback mClickCallback;
	public BoxColliderHoverCallback mHoverCallback;
	public BoxColliderPressCallback mPressCallback;
}

public class CompareFunc : IComparer<int>
{
	public int Compare(int a, int b)
	{
		if(a > b)
		{
			return -1;
		}
		else if(a < b)
		{
			return 1;
		}
		else
		{
			return 0;
		}
	}
}

public class GlobalTouchSystem : FrameComponent
{
	protected Dictionary<txUIObject, ColliderCallBack> mButtonCallbackList;
	protected SortedDictionary<int, List<txUIObject>> mButtonOrderList;	// 深度由大到小的列表
	protected Vector3 mLastMousePosition;
	protected txUIObject mHoverButton;
	protected bool mUseHover = true;		// 是否判断鼠标悬停在某个窗口
	public GlobalTouchSystem(string name)
		:base(name)
	{
		mButtonCallbackList = new Dictionary<txUIObject, ColliderCallBack>();
		mButtonOrderList = new SortedDictionary<int, List<txUIObject>>(new CompareFunc());
	}
	public override void init()
	{
		;
	}
	public override void destroy()
	{
		mButtonCallbackList.Clear();
		mButtonOrderList.Clear();
		base.destroy();
	}
	public Vector3 getCurMousePosition()
	{
		return Input.mousePosition;
	}
	public txUIObject getHoverButton(Vector3 pos)
	{
		// 返回Layout深度最大的,也就是最靠前的窗口
		List<txUIObject> boxList = globalRaycast(pos);
		txUIObject forwardButton = null;
		foreach(var button in boxList)
		{
			GameLayout layout = button.mLayout;
			if (forwardButton == null || layout.getRenderOrder() > forwardButton.mLayout.getRenderOrder())
			{
				forwardButton = button;
			}
		}
		return forwardButton;
	}
	public override void update(float elapsedTime)
	{
		if (!mUseHover)
		{
			return;
		}
		// 鼠标移动检测
		Vector3 curMousePosition = getCurMousePosition();
		if (mLastMousePosition != curMousePosition)
		{
			// 计算鼠标当前所在最前端的窗口
			txUIObject newWindow = getHoverButton(curMousePosition);
			// 判断鼠标是否还在当前窗口内
			if (mHoverButton != null)
			{
				// 鼠标已经移动到了其他窗口中,发送鼠标离开的事件
				if (newWindow != mHoverButton)
				{
					// 不过也许此时悬停窗口已经不接收输入事件了或者碰撞盒子被禁用了,需要判断一下
					if (mHoverButton.getHandleInput() && mHoverButton.getBoxCollider().enabled)
					{
						hoverWindow(mHoverButton, false);
					}
					// 找到鼠标所在的新的窗口,给该窗口发送鼠标进入的事件
					hoverWindow(newWindow, true);
				}
			}
			// 如果上一帧鼠标没有在任何窗口内,则计算这一帧鼠标所在的窗口
			else
			{
				// 发送鼠标进入的事件
				hoverWindow(newWindow, true);
			}
			mHoverButton = newWindow;
			mLastMousePosition = curMousePosition;
		}
	}
	// 注册碰撞器,只有注册了的碰撞器才会进行检测
	public void registeBoxCollider(txUIObject button, BoxColliderClickCallback clickCallback = null, 
		BoxColliderHoverCallback hoverCallback = null, BoxColliderPressCallback pressCallback = null)
	{
		if (!mButtonCallbackList.ContainsKey(button))
		{
			ColliderCallBack colliderCallback = new ColliderCallBack();
			colliderCallback.mButton = button;
			colliderCallback.mClickCallback = clickCallback;
			colliderCallback.mHoverCallback = hoverCallback;
			colliderCallback.mPressCallback = pressCallback;
			mButtonCallbackList.Add(button, colliderCallback);
			if(!mButtonOrderList.ContainsKey(button.getDepth()))
			{
				mButtonOrderList.Add(button.getDepth(), new List<txUIObject>());
			}
			mButtonOrderList[button.getDepth()].Add(button);
		}
	}
	// 注销碰撞器
	public void unregisteBoxCollider(txUIObject button)
	{
		if (mButtonCallbackList.ContainsKey(button))
		{
			mButtonCallbackList.Remove(button);
			mButtonOrderList[button.getDepth()].Remove(button);
		}
	}
	public void notifyButtonDepthChanged(txUIObject button, int lastDepth)
	{
		// 如果之前没有记录过,则不做判断
		if(!mButtonOrderList.ContainsKey(lastDepth) || !mButtonOrderList[lastDepth].Contains(button))
		{
			return;
		}
		// 移除旧的按钮
		mButtonOrderList[lastDepth].Remove(button);
		// 添加新的按钮
		if (!mButtonOrderList.ContainsKey(button.getDepth()))
		{
			mButtonOrderList.Add(button.getDepth(), new List<txUIObject>());
		}
		mButtonOrderList[button.getDepth()].Add(button);
	}
	public void notifyGlobalPress(bool press)
	{
		List<txUIObject> raycast = globalRaycast(getCurMousePosition());
		foreach (var button in raycast)
		{
			if (mButtonCallbackList[button].mPressCallback != null)
			{
				mButtonCallbackList[button].mPressCallback(button, press);
			}
		}
		if(!press)
		{
			// 检测所有拣选到的盒子
			foreach (var button in raycast)
			{
				if (mButtonCallbackList[button].mClickCallback != null)
				{
					mButtonCallbackList[button].mClickCallback(button);
				}
			}
			if (raycast.Count == 0)
			{
				GameScene gameScene = mGameSceneManager.getCurScene();
				gameScene.notifyScreenActived();
			}
		}
	}
	//--------------------------------------------------------------------------------------------------------------------------
	// 全局射线检测
	protected List<txUIObject> globalRaycast(Vector3 mousePos)
	{
		Ray ray = UnityUtility.getRay(mousePos);
		List<txUIObject> raycastRet = UnityUtility.raycast(ray, mButtonOrderList);
		return raycastRet;
	}
	protected void hoverWindow(txUIObject window, bool hover)
	{
		if (window != null)
		{
			window.setMouseHovered(hover);
			if (mButtonCallbackList[window].mHoverCallback != null)
			{
				mButtonCallbackList[window].mHoverCallback(window, hover);
			}
		}
	}
}
