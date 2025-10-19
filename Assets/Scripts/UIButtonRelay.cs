using UnityEngine;

public class UIButtonRelay : MonoBehaviour
{
    public void Retry() { GameManager.I?.BTN_Retry(); }
    public void Menu()  { GameManager.I?.BTN_Menu();  }
}