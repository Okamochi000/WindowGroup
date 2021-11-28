using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ウィンドウ切替管理
/// </summary>
public class WindowGroup : MonoBehaviour
{
    /// <summary>
    /// 切替方法
    /// </summary>
    public enum TransitionType
    {
        Single,         // 切替を順々に行う
        CrossDissolve,  // 切替を同時に行う
    }

    /// <summary>
    /// 開閉状態
    /// </summary>
    public enum OpenState
    {
        Closed,     // 閉じている
        Transtion,  // 遷移中
        Opened      // 開いている
    }

    public OpenState CurrentOpenState { get; private set; } = OpenState.Closed;

    [SerializeField] private List<OpenAndCloseBase> windowList = new List<OpenAndCloseBase>();
    [SerializeField] private TransitionType transitionType = TransitionType.CrossDissolve;
    [SerializeField] private OpenAndCloseBase background = null;
    [SerializeField] private GameObject frontClickBlocker = null;

    private List<int> windowHistory_ = new List<int>();
    private int activeIndex_ = -1;

    /// <summary>
    /// ウィンドウを開く
    /// </summary>
    /// <param name="windowNumber"></param>
    public void Open(int windowNumber)
    {
        // 同じウィンドウの場合は何もしない
        if (windowNumber == GetActiveWindowNumber()) { return; }

        // 遷移中は何もしない
        if (CurrentOpenState == OpenState.Transtion) { return; }

        // ウィンドウが存在しない場合は何もしない
        if (GetWindow(windowNumber) == null) { return; }

        // 開閉履歴調整
        if (activeIndex_ <= (windowHistory_.Count - 1))
        {
            int tempIndex = (activeIndex_ + 1);
            while (tempIndex < windowHistory_.Count) { windowHistory_.RemoveAt((windowHistory_.Count - 1)); }
        }
        windowHistory_.Add(windowNumber);

        // 遷移開始
        Transition(true);
    }

    /// <summary>
    /// 次に進む
    /// </summary>
    public void Next()
    {
        // 遷移中は何もしない
        if (CurrentOpenState == OpenState.Transtion) { return; }

        // 次のウィンドウがない場合は何もしない
        if (activeIndex_ >= (windowHistory_.Count -1)) { return; }

        // 遷移開始
        Transition(true);
    }

    /// <summary>
    /// 1つ前に戻る
    /// </summary>
    public void Back()
    {
        // 遷移中は何もしない
        if (CurrentOpenState == OpenState.Transtion) { return; }

        // 前のウィンドウがない場合は何もしない
        if (activeIndex_ < 1) { CloseAll(); }
        else { Transition(false); }
    }

    /// <summary>
    /// すべて閉じる
    /// </summary>
    public void CloseAll()
    {
        // 遷移中は何もしない
        if (CurrentOpenState == OpenState.Transtion) { return; }

        // 初期化
        windowHistory_.Clear();
        activeIndex_ = -1;

        // 遷移開始
        StartCoroutine(TransitionCloseAll());
    }

    /// <summary>
    /// 履歴を削除する
    /// </summary>
    public void ResetHistory()
    {
        int acitveNumber = GetActiveWindowNumber();
        activeIndex_ = -1;
        windowHistory_.Clear();
        if (acitveNumber >= 0)
        {
            activeIndex_ = 0;
            windowHistory_.Add(acitveNumber);
        }
    }

    /// <summary>
    /// アクティブなウィンドウ番号を取得
    /// </summary>
    /// <returns></returns>
    public int GetActiveWindowNumber()
    {
        if (activeIndex_ >= windowHistory_.Count || activeIndex_ < 0) { return -1; }
        return windowHistory_[activeIndex_];
    }

    /// <summary>
    /// ウィンドウを取得する
    /// </summary>
    /// <param name="windowNumber"></param>
    /// <returns></returns>
    public OpenAndCloseBase GetWindow(int windowNumber)
    {
        if (windowNumber >= windowList.Count || windowNumber < 0) { return null; }
        return windowList[windowNumber];
    }

    /// <summary>
    /// 現在アクティブなウィンドウを取得する
    /// </summary>
    /// <param name="windowNumber"></param>
    /// <returns></returns>
    public OpenAndCloseBase GetActiveWindow()
    {
        return GetWindow(GetActiveWindowNumber());
    }

    /// <summary>
    /// 全ウィンドウが閉じた
    /// </summary>
    protected virtual void OnClosedAllWindow() { }

    /// <summary>
    /// 遷移
    /// </summary>
    /// <param name="isNext"></param>
    private void Transition(bool isNext)
    {
        switch (transitionType)
        {
            case TransitionType.Single: StartCoroutine(TransitionSingle(isNext)); break;
            case TransitionType.CrossDissolve: StartCoroutine(TransitionCrossDissolve(isNext)); break;
            default: break;
        }
    }

    /// <summary>
    /// 全てのウィンドウを閉じる
    /// </summary>
    /// <returns></returns>
    private IEnumerator TransitionCloseAll()
    {
        // 開閉状態変更
        CurrentOpenState = OpenState.Transtion;

        // 背景を開く
        if (background != null) { background.Close(); }

        // 前面クリックブロッカーを表示する
        if (frontClickBlocker != null) { frontClickBlocker.SetActive(true); }

        // 現在のウィンドウを閉じる
        List<OpenAndCloseBase> activeWindowList = new List<OpenAndCloseBase>();
        foreach (OpenAndCloseBase window in windowList)
        {
            if (window != null && window.IsOpen())
            {
                window.Close();
                activeWindowList.Add(window);
            }
        }

        // 全てのウィンドウが閉じるまで待つ
        bool isAllClosed = (activeWindowList.Count == 0);
        while (!isAllClosed)
        {
            isAllClosed = true;
            foreach (OpenAndCloseBase window in activeWindowList)
            {
                if (window.IsOpen())
                {
                    isAllClosed = false;
                    break;
                }
            }

            yield return null;
        }

        // 背景が閉じ切るまで待つ
        if (background != null)
        {
            while (background.IsOpen()) { yield return null; }
        }

        // 前面クリックブロッカーを非表示にする
        if (frontClickBlocker != null) { frontClickBlocker.SetActive(false); }

        // 開閉状態変更
        CurrentOpenState = OpenState.Closed;

        // コールバック呼び出し
        OnClosedAllWindow();
    }

    /// <summary>
    /// 順々にウィンドウを切り替える
    /// </summary>
    /// <param name="isNext"></param>
    /// <returns></returns>
    private IEnumerator TransitionSingle(bool isNext)
    {
        // 開閉状態変更
        CurrentOpenState = OpenState.Transtion;

        // 背景を開く
        if (background != null) { background.Open(); }

        // 前面クリックブロッカーを表示する
        if (frontClickBlocker != null) { frontClickBlocker.SetActive(true); }

        // 現在のウィンドウを閉じる
        OpenAndCloseBase activeWindow = GetActiveWindow();
        if (activeWindow != null)
        {
            activeWindow.Close();
            while (activeWindow.CurrentOpenState != OpenAndCloseBase.OpenState.Closed) { yield return null; }
        }

        // 次のウィンドウが開かれるまで待つ
        if (isNext) { activeIndex_++; }
        else { activeIndex_--; }
        OpenAndCloseBase nextWindow = GetActiveWindow();
        nextWindow.Open();
        while (nextWindow.CurrentOpenState != OpenAndCloseBase.OpenState.Opened) { yield return null; }

        // 背景が開ききるまで待つ
        if (background != null)
        {
            while (background.CurrentOpenState != OpenAndCloseBase.OpenState.Opened) { yield return null; }
        }

        // 前面クリックブロッカーを非表示にする
        if (frontClickBlocker != null) { frontClickBlocker.SetActive(false); }

        // 開閉状態変更
        CurrentOpenState = OpenState.Opened;
    }

    /// <summary>
    /// 同時にウィンドウを切り替える
    /// </summary>
    /// <param name="isNext"></param>
    /// <returns></returns>
    private IEnumerator TransitionCrossDissolve(bool isNext)
    {
        // 開閉状態変更
        CurrentOpenState = OpenState.Transtion;

        // 背景を開く
        if (background != null) { background.Open(); }

        // 前面クリックブロッカーを表示する
        if (frontClickBlocker != null) { frontClickBlocker.SetActive(true); }

        // 現在のウィンドウを閉じる
        OpenAndCloseBase activeWindow = GetActiveWindow();
        if (activeWindow != null) { activeWindow.Close(); }

        // 次のウィンドウが開かれるまで待つ
        if (isNext) { activeIndex_++; }
        else { activeIndex_--; }
        OpenAndCloseBase nextWindow = GetActiveWindow();
        nextWindow.Open();

        // 現在のウィンドウが閉じ切るまで待つ
        if (activeWindow != null)
        {
            while (activeWindow.CurrentOpenState != OpenAndCloseBase.OpenState.Closed) { yield return null; }
        }

        // 次のウィンドウが開ききるまで待つ
        while (nextWindow.CurrentOpenState != OpenAndCloseBase.OpenState.Opened) { yield return null; }

        // 背景が開ききるまで待つ
        if (background != null)
        {
            while (background.CurrentOpenState != OpenAndCloseBase.OpenState.Opened) { yield return null; }
        }

        // 前面クリックブロッカーを非表示にする
        if (frontClickBlocker != null) { frontClickBlocker.SetActive(false); }

        // 開閉状態変更
        CurrentOpenState = OpenState.Opened;
    }
}