//using UnityEngine;
//using UnityEngine.UI;

///// Floating health bar. Put it on a world space canvas under any unit (alien or soldier).
//public class UnitHealthBar : MonoBehaviour
//{
//    [Header("UI References")]
//    public Image healthBarFill;

//    [Header("Unit Reference")]
//    public Unit unit;

//    private Camera mainCamera;

//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        mainCamera = Camera.main;

//        if (unit == null)
//        {
//            unit = GetComponentInParent<Unit>();
//        }
//    }

//    void LateUpdate()
//    {
//        // Make the health bar face the camera
//        if (mainCamera != null)
//        {
//            transform.rotation = mainCamera.transform.rotation;
//        }

//        // Update the health bar
//        if (unit != null && healthBarFill != null)
//        {
//            float healthPercent =
//                (float)unit.Health /
//                Mathf.Max(1, unit.maxHealth);

//            healthBarFill.fillAmount =
//                Mathf.Clamp01(healthPercent);
//        }
//    }
//}