using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 照片书控制器：按J打开，按AD或点击翻页，按ESC/J关闭
/// </summary>
public class PhotoBookController : MonoBehaviour
{
    // 照片书的画布
    public Canvas bookCanvas;

    // 照片显示的图片组件
    public Image photoImage;

    // 左右翻页按钮
    public Button leftButton;
    public Button rightButton;

    // 照片列表
    public List<Sprite> photos = new List<Sprite>();

    // 当前照片索引
    private int currentIndex = 0;

    // 照片书是否打开
    private bool isBookOpen = false;

    // 保存的游戏时间缩放
    private float savedTimeScale;

    // 需要在打开书时禁用的组件
    public List<Behaviour> componentsToDisable;

    void Start()
    {
        // 初始化时关闭书
        if (bookCanvas != null)
            bookCanvas.gameObject.SetActive(false);

        // 设置按钮点击事件
        if (leftButton != null)
            leftButton.onClick.AddListener(PreviousPhoto);

        if (rightButton != null)
            rightButton.onClick.AddListener(NextPhoto);
    }

    void Update()
    {
        // 按J键打开/关闭书
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isBookOpen)
                CloseBook();
            else
                OpenBook();
        }

        // 按ESC或F键关闭书
        if (isBookOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)))
        {
            CloseBook();
        }

        // 在书打开时处理翻页
        if (isBookOpen)
        {
            // 按A键查看上一张
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousPhoto();
            }

            // 按D键查看下一张
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextPhoto();
            }
        }
    }

    // 打开书
    void OpenBook()
    {
        if (bookCanvas == null) return;

        // 打开书
        isBookOpen = true;
        bookCanvas.gameObject.SetActive(true);

        // 暂停游戏
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0;

        // 显示鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 禁用特定组件
        foreach (var component in componentsToDisable)
        {
            if (component != null)
                component.enabled = false;
        }

        // 显示第一张照片
        currentIndex = 0;
        ShowPhoto();
    }

    // 关闭书
    void CloseBook()
    {
        if (bookCanvas == null) return;

        // 关闭书
        isBookOpen = false;
        bookCanvas.gameObject.SetActive(false);

        // 恢复游戏速度
        Time.timeScale = savedTimeScale;

        // 隐藏鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 重新启用组件
        foreach (var component in componentsToDisable)
        {
            if (component != null)
                component.enabled = true;
        }
    }

    // 显示当前照片
    void ShowPhoto()
    {
        if (photoImage == null || photos.Count == 0) return;

        // 确保索引有效
        if (currentIndex < 0)
            currentIndex = 0;
        if (currentIndex >= photos.Count)
            currentIndex = photos.Count - 1;

        // 显示照片
        photoImage.sprite = photos[currentIndex];

        // 更新按钮状态
        UpdateButtons();
    }

    // 显示上一张照片
    public void PreviousPhoto()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowPhoto();
        }
    }

    // 显示下一张照片
    public void NextPhoto()
    {
        if (currentIndex < photos.Count - 1)
        {
            currentIndex++;
            ShowPhoto();
        }
    }

    // 更新按钮状态
    void UpdateButtons()
    {
        if (leftButton != null)
            leftButton.interactable = (currentIndex > 0);

        if (rightButton != null)
            rightButton.interactable = (currentIndex < photos.Count - 1);
    }
}