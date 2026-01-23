using Duckov.MiniMaps.UI;
using LeTai.TrueShadow;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace MiniMap.Poi
{
    public class CharacterPoiEntry : MonoBehaviour
    {
        private RectTransform? rectTransform;

        private MiniMapDisplay? master;

        private CharacterPoi? target;

        //private MiniMapDisplayEntry? minimapEntry;

        [SerializeField]
        private Transform? indicatorContainer;

        [SerializeField]
        private Transform? iconContainer;

        [SerializeField]
        private Sprite? defaultIcon;

        [SerializeField]
        private Color defaultColor = Color.white;

        [SerializeField]
        private Image? icon;

        [SerializeField]
        private Transform? direction;

        [SerializeField]
        private Image? arrow;

        [SerializeField]
        private TrueShadow? shadow;

        [SerializeField]
        private TextMeshProUGUI? displayName;

        [SerializeField]
        private ProceduralImage? areaDisplay;

        [SerializeField]
        private Image? areaFill;

        [SerializeField]
        private float areaLineThickness = 1f;

        [SerializeField]
        private string? caption;

        private Vector3 cachedWorldPosition = Vector3.zero;

        public CharacterPoi? Target => target;

        private float ParentLocalScale => transform?.parent?.localScale.x ?? 1f;

        public void Initialize(CharacterPoiEntryData entryData)
        {
            areaDisplay = entryData.areaDisplay;
            areaFill = entryData.areaFill;
            indicatorContainer = entryData.indicatorContainer;
            iconContainer = entryData.iconContainer;
            icon = entryData.icon;
            shadow = entryData.shadow;
            direction = entryData.direction;
            arrow = entryData.arrow;
            displayName = entryData.displayName;
        }

        internal void Setup(MiniMapDisplay master, CharacterPoi target)
        {
            //ModBehaviour.Logger.Log($"开始初始化 CharacterPoiEntry");
            rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
            this.master = master;
            this.target = target;
            caption = target.name;
            var displayNameRect = displayName?.transform as RectTransform;
            if (target.HideIcon)
            {
                if (displayNameRect != null)
                {
                    iconContainer?.gameObject.SetActive(false);
                    displayNameRect.pivot = new Vector2(0.5f, 0.5f);
                    displayNameRect.anchorMin = new Vector2(0.5f, 0.5f);
                    displayNameRect.anchorMax = new Vector2(0.5f, 0.5f);
                    displayName!.alignment = TextAlignmentOptions.Midline;
                }
            }
            else
            {
                if (displayNameRect != null)
                {
                    iconContainer?.gameObject.SetActive(true);
                    displayNameRect.pivot = new Vector2(0.5f, 1f);
                    displayNameRect.anchorMin = new Vector2(0.5f, 0f);
                    displayNameRect.anchorMax = new Vector2(0.5f, 0f);
                    displayName!.alignment = TextAlignmentOptions.Top;
                }
            }
            direction?.gameObject.SetActive(!target.HideArrow);
            if (icon != null)
            {
                //ModBehaviour.Logger.Log("设置图标");
                icon.sprite = target.Icon ?? defaultIcon;
                icon.color = target.Color;
            }
            if (arrow != null)
            {
                //ModBehaviour.Logger.Log("设置箭头");
                arrow.sprite = target.Arrow;
                arrow.color = target.ArrowColor;
                arrow.transform.localScale = Vector3.one * target.ArrowScaleFactor;
            }
            if (shadow != null)
            {
                //ModBehaviour.Logger.Log("设置图标阴影");
                shadow.Color = target.ShadowColor;
                shadow.OffsetDistance = target.ShadowDistance;
            }

            if (displayName != null)
            {
                //ModBehaviour.Logger.Log("设置图标名称");
                caption = target.DisplayName;
                displayName.gameObject.SetActive(value: true);
                displayName.text = target.DisplayName;
            }

            if (areaDisplay != null && areaFill != null)
            {
                //ModBehaviour.Logger.Log("设置范围显示");
                areaDisplay.color = defaultColor;
                Color color = defaultColor;
                color.a *= 0.1f;
                areaFill.color = color;
                if (target.IsArea)
                {
                    areaDisplay.gameObject.SetActive(value: true);
                    rectTransform!.sizeDelta = target.AreaRadius * Vector2.one * 2f;
                    areaDisplay.color = target.Color;
                    color = target.Color;
                    color.a *= 0.1f;
                    areaFill.color = color;
                    areaDisplay.BorderWidth = areaLineThickness / ParentLocalScale;
                }
                else
                {
                    areaDisplay.gameObject.SetActive(value: false);
                }
            }

            RefreshPosition();
            enabled = true;
            gameObject.SetActive(value: true);
        }

        private void RefreshPosition()
        {
            try
            {
                if (target != null && master != null && master.TryConvertWorldToMinimap(target.transform.position, SceneInfoCollection.GetSceneID(SceneManager.GetActiveScene().buildIndex), out Vector3 minimapPos))
                {
                    cachedWorldPosition = target.transform.position;
                    Vector3 position = master.transform.localToWorldMatrix.MultiplyPoint(minimapPos);
                    transform.position = position;
                    UpdateScale();
                    UpdateRotation();
                }
            }
            catch { }
        }

        private void Update()
        {
            if (target == null || target.IsDestroyed())
            {
                //Destroy(this);
                return;
            }
            UpdateScale();
            UpdatePosition();
            UpdateRotation();
        }

        private void UpdateScale()
        {
            if (indicatorContainer != null && areaDisplay != null)
            {
                float num = target?.IconScaleFactor ?? 1f;
                indicatorContainer.localScale = Vector3.one * num / ParentLocalScale;
                if (target != null && target.IsArea)
                {
                    areaDisplay.BorderWidth = areaLineThickness / ParentLocalScale;
                    areaDisplay.FalloffDistance = 1f / ParentLocalScale;
                }
            }
        }

        private void UpdatePosition()
        {
            if (cachedWorldPosition != target?.transform.position)
            {
                RefreshPosition();
            }
        }

        private void UpdateRotation()
        {
            transform.rotation = Quaternion.identity;
            if (target != null && direction != null && master != null && !target.HideArrow)
            {
                direction.rotation = Quaternion.Euler(0f, 0f, target.RotationEulerAngle + master.transform.rotation.eulerAngles.z);
            }
        }
    }
}