using UnityEngine;

/// <summary>
/// 開閉ベース
/// </summary>
public class OpenAndCloseBase : MonoBehaviour
{
    /// <summary>
    /// 開閉状態
    /// </summary>
    public enum OpenState
    {
        OpenAnim,
        Opened,
        CloseAnim,
        Closed
    }

    /// <summary>
    /// 開閉アニメーションの種類
    /// </summary>
    protected enum OpenAnimType
    {
        None,
        Animator,
        Script
    }

    /// <summary>
    /// 開閉操作の種類
    /// </summary>
    private enum OpenActionType
    {
        None,
        Open,
        Close
    }

    public OpenState CurrentOpenState { get; private set; } = OpenState.Closed;

    [SerializeField] protected OpenAnimType openAnimType = OpenAnimType.None;
    [SerializeField] protected bool isAutoActive = true;

    private OpenActionType nextOpenAction_ = OpenActionType.None;

    public virtual void OnEnable()
    {
        // Openを使用しないときの自動開閉
        if (!isAutoActive) { return; }
        if (!this.gameObject.activeSelf || CurrentOpenState != OpenState.Closed) { return; }
        Open();
    }

    public virtual void OnDisable()
    {
        // Closeを使用しないときの自動開閉
        if (isAutoActive)
        {
            if (!this.gameObject.activeSelf) { Close(); }
        }

        // Animatorが指定されていた場合アニメーションを終了
        if (openAnimType == OpenAnimType.Animator)
        {
            if (CurrentOpenState == OpenState.OpenAnim) { ChangeOpenState(OpenState.Opened); }
            else if (CurrentOpenState == OpenState.CloseAnim) { ChangeOpenState(OpenState.Closed); }
        }
    }

    /// <summary>
    /// 開く
    /// </summary>
    public void Open()
    {
        // 閉じ切っていない場合は閉じ切るまで待つ
        if (CurrentOpenState != OpenState.Closed)
        {
            nextOpenAction_ = OpenActionType.Open;
            return;
        }

        // 開閉アニメーション更新
        nextOpenAction_ = OpenActionType.None;
        ChangeOpenState(OpenState.OpenAnim, true);

        // 開ききっている、もしくはスクリプト準拠の場合は終了
        if (CurrentOpenState == OpenState.Opened) { return; }
        if (openAnimType == OpenAnimType.Script) { return; }

        // アニメーションの終了チェック
        bool isFinishedAnim = true;
        if (openAnimType == OpenAnimType.Animator)
        {
            if (this.gameObject.activeInHierarchy)
            {
                // アクティブ状態ならアニメーショントリガー設定
                Animator animator = this.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetTrigger("Open");
                    isFinishedAnim = false;
                }
            }
        }
        if (isFinishedAnim) { ChangeOpenState(OpenState.Opened, true); }
    }

    /// <summary>
    /// 閉じる
    /// </summary>
    public void Close()
    {
        // 開ききっていない場合は開ききるまで待つ
        if (CurrentOpenState != OpenState.Opened)
        {
            nextOpenAction_ = OpenActionType.Close;
            return;
        }

        // 開閉アニメーション更新
        nextOpenAction_ = OpenActionType.None;
        ChangeOpenState(OpenState.CloseAnim, true);

        // 閉じ切っている、もしくはスクリプト準拠の場合は終了
        if (CurrentOpenState == OpenState.Closed) { return; }
        if (openAnimType == OpenAnimType.Script) { return; }

        // アニメーションの終了チェック
        bool isFinishedAnim = true;
        if (openAnimType == OpenAnimType.Animator)
        {
            if (this.gameObject.activeInHierarchy)
            {
                // アクティブ状態ならアニメーショントリガー設定
                Animator animator = this.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetTrigger("Close");
                    isFinishedAnim = false;
                }
            }
        }
        if (isFinishedAnim) { ChangeOpenState(OpenState.Closed, true); }
    }

    /// <summary>
    /// 開閉状態を取得する
    /// </summary>
    /// <returns></returns>
    public bool IsOpen()
    {
        return (CurrentOpenState != OpenState.Closed);
    }

    /// <summary>
    /// 開閉状態切替
    /// </summary>
    /// <param name="state"></param>
    /// <param name="isCallback"></param>
    protected void ChangeOpenState(OpenState state, bool isCallback = true)
    {
        // ステータスが同じなら何もしない
        if (CurrentOpenState == state) { return; }

        // 開閉状態切替
        CurrentOpenState = state;

        // 表示状態切替
        if (isAutoActive && Application.isPlaying)
        {
            if (CurrentOpenState == OpenState.OpenAnim) { this.gameObject.SetActive(true); }
            else if (CurrentOpenState == OpenState.Closed) { this.gameObject.SetActive(false); }
        }

        // コールバック呼び出し
        if (isCallback) { OnChangedOpenState(state); }

        // 次の操作呼び出し
        if (CurrentOpenState == OpenState.Opened)
        {

            if (nextOpenAction_ == OpenActionType.Close) { Close(); }
            nextOpenAction_ = OpenActionType.None;
        }
        else if (CurrentOpenState == OpenState.Closed)
        {
            if (nextOpenAction_ == OpenActionType.Open) { Open(); }
            nextOpenAction_ = OpenActionType.None;
        }
    }

    /// <summary>
    /// 開閉状態変更コールバック
    /// </summary>
    /// <param name="state"></param>
    protected virtual void OnChangedOpenState(OpenState state) { }

    /// <summary>
    /// アニメーショントリガー
    /// </summary>
    /// <param name="eventName"></param>
    private void OnAnimationTrigger(string eventName)
    {
        if (openAnimType != OpenAnimType.Animator) { return; }

        // トリガーリセット
        Animator animator = this.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger("Open");
            animator.ResetTrigger("Close");
        }

        // 遷移
        if (eventName == "Open") { ChangeOpenState(OpenState.Opened, true); }
        else if (eventName == "Close") { ChangeOpenState(OpenState.Closed, true); }
    }
}
