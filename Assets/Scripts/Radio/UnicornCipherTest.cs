using UnityEngine;

public class UnicornCipherTest : MonoBehaviour
{

   // public CipherController cipherController;
    public GameObject cipherManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnButtonClick()
    {
        cipherManager.SetActive(true);
    }
}
