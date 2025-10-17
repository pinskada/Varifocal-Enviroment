using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UICreateProfile : MonoBehaviour
{
    [SerializeField] private GameObject newProfilePanel;
    [SerializeField] private TMP_InputField profileNameInputField;
    [SerializeField] private Button createProfileButton;
    [SerializeField] private Button cancelButton;

    private Action<string> onProfileConfirmed;


    private void Awake()
    {
        newProfilePanel.SetActive(false);
        createProfileButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }


    public void PromptUserForProfileName(Action<string> onConfirm)
    {
        onProfileConfirmed = onConfirm;
        profileNameInputField.text = "";
        newProfilePanel.SetActive(true);
        profileNameInputField.Select();
        profileNameInputField.ActivateInputField();
    }


    private void OnConfirmClicked()
    {
        string name = profileNameInputField.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[UICreateProfile] Profile name cannot be empty");
            return;
        }

        newProfilePanel.SetActive(false);
        onProfileConfirmed?.Invoke(name);
        onProfileConfirmed = null;
    }


    private void OnCancelClicked()
    {
        newProfilePanel.SetActive(false);
        onProfileConfirmed?.Invoke(null);
        onProfileConfirmed = null;
    }

}
