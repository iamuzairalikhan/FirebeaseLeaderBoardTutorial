using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using TMPro;
using UnityEditor.Rendering;
using NUnit.Framework;

public class FirebaseLeaderboardManager : MonoBehaviour
{

    public GameObject usernamePanel, userProfilePanel, leaderboardPanel, leaderboardContent, userDataPrefab;

    public TMP_Text profileUsernameTxt, profileUserScoreTxt, errorUsernameTxt;

    public TMP_InputField usernameInput;

    public int score, totalUsers = 0;

    public string username = "";

    private DatabaseReference db;

    void Start()
    {
        FirebaseInitialize();
    }

    void Update()
    {
        
    }

    public void ShowLeaderboard()
    {
        StartCoroutine(FetchLeaderboardData());
    }

    public void SignInWithUsername()
    {
        StartCoroutine(CheckUserExistInDatabase());
    }

   public void CloseLeaderboard()
   {
        if(leaderboardContent.transform.childCount > 0)
        {
            for(int i = 0; i < leaderboardContent.transform.childCount; i++)
            {
                Destroy(leaderboardContent.transform.GetChild(i).gameObject);
            }
        }
        leaderboardPanel.SetActive(false);
        userProfilePanel.SetActive(true);
   } 

   public void SignOut()
   {
        PlayerPrefs.DeleteKey("PlayerID");
        PlayerPrefs.DeleteKey("Username");
        usernameInput.text = "";
        profileUsernameTxt.text = "";
        profileUserScoreTxt.text = "";
        score = 0;
        username = "";

        usernamePanel.SetActive(true);
        userProfilePanel.SetActive(false);
   }

    void FirebaseInitialize()
    {
        db = FirebaseDatabase.DefaultInstance.GetReference("/LeaderBoard/");

        db.ChildAdded += HandleChildAdded;

        GetTotalUsers();

        StartCoroutine(FetchUserProfileData(PlayerPrefs.GetInt("PlayerID")));
    }
    
    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if(args.DatabaseError != null)
        {
            return;
        }

        GetTotalUsers();
    }

    void GetTotalUsers()
    {
        db.ValueChanged +=  (object sender2, ValueChangedEventArgs e2) => 
        {
            if(e2.DatabaseError != null)
            {
                Debug.Log(e2.DatabaseError.Message);
                return;
            }

            totalUsers = int.Parse(e2.Snapshot.ChildrenCount.ToString());

            Debug.LogError("Total Users : " + totalUsers);
        };
    }

    IEnumerator CheckUserExistInDatabase()
    {
        var task = db.OrderByChild("username").EqualTo(usernameInput.text).GetValueAsync();
        yield return new WaitUntil(()=>task.IsCompleted);

        if(task.IsFaulted)
        {
            errorUsernameTxt.text = "Invalid Error";
        }
        else if(task.IsCompleted) 
        {
            DataSnapshot snapshot = task.Result;

            if(snapshot != null && snapshot.HasChildren)
            {
                Debug.LogError("Username Exist");

                errorUsernameTxt.text = "Username Already Exist";
            }
            else
            {
                Debug.LogError("Username Not Exist");

                PushUserData();
                PlayerPrefs.SetInt("PlayerID", totalUsers+1);
                PlayerPrefs.SetString("Username", usernameInput.text);

                StartCoroutine(DelayFetchData());
            }
        }

    }

    IEnumerator DelayFetchData()
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(FetchUserProfileData(totalUsers));
    }

    void PushUserData()
    {
        db.Child("user_" + (totalUsers+1).ToString()).Child("username").SetValueAsync(usernameInput.text);
        db.Child("user_" + (totalUsers+1).ToString()).Child("score").SetValueAsync(750);
    }

    IEnumerator FetchUserProfileData(int playerID)
    {
        Debug.LogError(playerID);
        if(playerID != 0)
        {
            var task = db.Child("user_"+playerID.ToString()).GetValueAsync();
            yield return new WaitUntil(()=>task.IsCompleted);

            if(task.IsFaulted)
            {
                Debug.Log("Invalid Error");

            }
            else if(task.IsCompleted) 
            {
                DataSnapshot snapshot = task.Result;

                if(snapshot != null && snapshot.HasChildren)
                {
                    username = snapshot.Child("username").Value.ToString();
                    score = int.Parse(snapshot.Child("score").Value.ToString());

                    profileUsernameTxt.text = username;
                    profileUserScoreTxt.text = ""+score;
                    userProfilePanel.SetActive(true);
                    usernamePanel.SetActive(false);
                }
                else
                {
                    Debug.LogError("User ID Not Exist");
                }
            }
        }
        yield return null;
    }

    IEnumerator FetchLeaderboardData()
    {
        Debug.Log("Fetch Leaderboard");
        var task = db.OrderByChild("score").LimitToFirst(10).GetValueAsync();
        yield return new WaitUntil(()=>task.IsCompleted);
        
        if(task.IsFaulted)
        {
            Debug.Log("Invalid Error");

        }
        else if(task.IsCompleted) 
        {
            Debug.LogError("ShowLeaderboard");
            DataSnapshot snapshot = task.Result;

            Debug.LogError(snapshot.ChildrenCount);

            List<LeaderbaordData> listLeaderboardEntry = new List<LeaderbaordData>();

            foreach(DataSnapshot childSnapshot in snapshot.Children)
            {
                string username2 = childSnapshot.Child("username").Value.ToString();
                int score = int.Parse(childSnapshot.Child("score").Value.ToString());

                Debug.LogError(username2 + " || " + score);

                listLeaderboardEntry.Add(new LeaderbaordData(username2, score));
            }

            DisplayLeaderboardData(listLeaderboardEntry);
        }
    }

    void DisplayLeaderboardData(List<LeaderbaordData> leaderboardData)
    {
        int rankCount = 0;

        for(int i = leaderboardData.Count - 1; i >= 0; i--)
        {
            rankCount = rankCount + 1;

            GameObject obj = Instantiate(userDataPrefab);
            obj.transform.parent = leaderboardContent.transform;
            obj.transform.localScale = Vector3.one;

            obj.GetComponent<UserDataUI>().userRankTxt.text = "Rank" + rankCount;
            obj.GetComponent<UserDataUI>().usernameTxt.text = "" + leaderboardData[i].username; 
            obj.GetComponent<UserDataUI>().userScoreTxt.text = "" + leaderboardData[i].score;
        }

        leaderboardPanel.SetActive(true);
        userProfilePanel.SetActive(false);
    }
}

public class LeaderbaordData
{
    public string username;
    public int score;

    public LeaderbaordData(string username, int score)
    {
        this.username = username;
        this.score = score;
    }
}
