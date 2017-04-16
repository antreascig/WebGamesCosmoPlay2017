﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebGames.Helpers;
using WebGames.Models;

namespace WebGames.Libs.Games
{
    public class GameTime
    {
        public int timeInSeconds { get; set; }
        public long timeStamp { get; set; }
    }

    public class ActivityManager
    {
        const int allowedTime = 5 * 60; // 5 minutes

        public static void SyncPlayedTimeForToday(string UserId, int remainingTime, long timeStamp)
        {
            var timeInSeconds = allowedTime - remainingTime;
            if (timeInSeconds < 0) return;
            if (timeInSeconds > allowedTime) timeInSeconds = allowedTime;

            SavePlayTime(UserId, DateTime.UtcNow.Date, timeInSeconds, timeStamp);
        }
        // timeStamp = JS Date().getTime() - epoch time
        public static void SavePlayTime(string UserId, DateTime Day, int TimePlayed, long jsTimeStamp, bool Override = false)
        {         
            using (var db = ApplicationDbContext.Create())
            {
                UserDailyActivity activity = GetorCreateActivity(UserId, Day, db, CreateIfNotExists:true);

                if (jsTimeStamp > activity.Timestamp && activity.TimePlayed < TimePlayed)
                {
                    activity.TimePlayed = TimePlayed;
                    activity.Timestamp = jsTimeStamp;

                    db.SaveChanges();
                }
                else if (Override)
                {
                    activity.TimePlayed = TimePlayed;
                    activity.Timestamp = jsTimeStamp;

                    db.SaveChanges();
                }
            }
        }

        public static GameTime GetGameTime(string UserId, DateTime Day)
        {
            var res = new GameTime()
            {
                timeInSeconds = 0,
                timeStamp = 0
            };
            using (var db = ApplicationDbContext.Create())
            {
                UserDailyActivity activity = GetorCreateActivity(UserId, Day, db, CreateIfNotExists: false);
                if (activity != null)
                {
                    res.timeInSeconds = activity.TimePlayed;
                    res.timeStamp = activity.Timestamp;
                }
            }
            return res;
        }

        private static UserDailyActivity GetorCreateActivity(string UserId, DateTime Day, ApplicationDbContext db, bool CreateIfNotExists = false)
        {
            var localizedDate = DateHelper.GetGreekDate(Day, onlyDate: true);

            var activity = db.UserDailyActivity.FindAsync(UserId, localizedDate).Result;
            if (activity == null && CreateIfNotExists)
            {
                activity = new UserDailyActivity()
                {
                    Date = localizedDate,
                    UserId = UserId,
                    TimePlayed = 0,
                    Timestamp = 0
                };

                db.UserDailyActivity.Add(activity);
            }

            return activity;
        }
    }
}