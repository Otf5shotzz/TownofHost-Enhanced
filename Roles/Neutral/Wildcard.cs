using UnityEngine;
using AmongUsMod.Mods;
using AmongUsMod.Roles;
using System.Collections.Generic;

public class WildCardRole : CustomRole
{
    public bool IsWithImposters { get; private set; }
    public bool IsWithCrewmates { get; private set; }

    public float SwiftAidCooldown { get; private set; } = 60f;
    public float DevourCooldown { get; private set; } = 60f;
    public float ShapeShifterCooldown { get; private set; } = 90f;

    public int TaskTrackerUses { get; private set; } = 1;
    public bool TaskTrackerVisible { get; private set; } = false;

    private bool canUseShapeShifter = true;
    private bool hasUsedShapeShifter = false;

    public WildCardRole()
    {
        this.RoleName = "Wild Card";
        this.RoleColor = Color.gray;
        
        if (UnityEngine.Random.value > 0.5f)
        {
            IsWithImposters = true;
            IsWithCrewmates = false;
        }
        else
        {
            IsWithImposters = false;
            IsWithCrewmates = true;
            canUseShapeShifter = false;
        }
    }

    public override void OnStart()
    {
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (!canUseShapeShifter && PlayerControl.LocalPlayer.Data.IsDead)
        {
            canUseShapeShifter = true;
        }
    }

    public void SwiftAid(Player player)
    {
        if (IsWithCrewmates && Time.time - lastSwiftAidTime >= SwiftAidCooldown)
        {
            lastSwiftAidTime = Time.time;
            var task = player.GetCurrentTask();
            if (task != null)
            {
                player.CompleteTask(task);
            }
        }
    }

    public void Devour(Player player)
    {
        if (IsWithImposters && Time.time - lastDevourTime >= DevourCooldown)
        {
            lastDevourTime = Time.time;
            var body = FindClosestBody(player);
            if (body != null)
            {
                body.Remove();
            }
        }
    }

    public void ShapeShifter(Player player)
    {
        if (IsWithImposters && !hasUsedShapeShifter && canUseShapeShifter)
        {
            hasUsedShapeShifter = true;
            canUseShapeShifter = false;

            player.RpcSetInfected(PlayerControl.LocalPlayer.PlayerId);
        }
    }

    public void TaskTracker(Player player, string playerId)
    {
        if (TaskTrackerUses > 0)
        {
            TaskTrackerUses--;
            TaskView(player, playerId);
        }
    }

    private void TaskView(Player player, string playerId)
    {
        Debug.LogFormat("Wild Card viewed tasks of player with ID: {0}", playerId);

        Player targetPlayer = GetPlayerById(playerId);
        if (targetPlayer != null)
        {
            List<TaskTypes> tasks = GetPlayerTasks(targetPlayer);

            foreach (var task in tasks)
            {
                bool isTaskCompleted = targetPlayer.TaskComplete(task);
                Debug.LogFormat("Task: {0} - Status: {1}", task, isTaskCompleted ? "Completed" : "Not Completed");

                if (isTaskCompleted)
                {
                    Debug.Log("Task completed! Implement game logic here.");
                }
                else
                {
                    Debug.Log("Task not completed! Implement alternative game logic here.");
                }
            }

            TaskTrackerVisible = true;
        }
    }

    private List<TaskTypes> GetPlayerTasks(Player player)
    {
        List<TaskTypes> tasks = new List<TaskTypes>();

        if (IsWithImposters)
        {
            tasks = GetRandomCompletedTasks(player.myTasks.Count);
        }
        else if (IsWithCrewmates)
        {
            tasks = player.myTasks;
        }

        return tasks;
    }

    private List<TaskTypes> GetRandomCompletedTasks(int numTasks)
    {
        List<TaskTypes> tasks = new List<TaskTypes>();

        for (int i = 0; i < numTasks; i++)
        {
            tasks.Add((TaskTypes)Random.Range(0, System.Enum.GetValues(typeof(TaskTypes)).Length));
        }

        return tasks;
    }

    private Player GetPlayerById(string playerId)
    {
        Player[] players = FindObjectsOfType<Player>();
        foreach (var player in players)
        {
            if (player.PlayerId == playerId)
            {
                return player;
            }
        }
        return null;
    }

    private DeadBody FindClosestBody(Player player)
    {
        DeadBody closestBody = null;
        float minDistance = float.MaxValue;

        foreach (var body in DeadBody.AllDeadBodies)
        {
            float distance = Vector3.Distance(player.transform.position, body.transform.position);
            if (distance < minDistance)
            {
                closestBody = body;
                minDistance = distance;
            }
        }

        return closestBody;
    }
}
