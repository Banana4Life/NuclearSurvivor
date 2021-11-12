using UnityEngine;

public class PauseMenu : MonoBehaviour
{
   public void Pause()
   {
      gameObject.SetActive(true);
      // TODO actually pause
   }
   
   public void Unpause()
   {
      gameObject.SetActive(false);
      // TODO actually unpause
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
