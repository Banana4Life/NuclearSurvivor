using UnityEngine;

public class PauseMenu : MonoBehaviour
{
   public void Pause()
   {
      Time.timeScale = 0;      
      gameObject.SetActive(true);
   }
   
   public void Unpause()
   {
      Time.timeScale = 1;
      gameObject.SetActive(false);
   }
   
   public void ExitButton()
   {
      #if UNITY_EDITOR
         UnityEditor.EditorApplication.isPlaying = false;
         return;
      #endif
      Application.Quit();
   }
   
   public void TogglePause()
   {
      if (gameObject.activeSelf)
      {
         Unpause();
      }
      else
      {
         Pause();
      }
   }
}
