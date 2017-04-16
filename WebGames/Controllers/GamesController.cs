﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using WebGames.Libs;
using WebGames.Libs.Games;
using WebGames.Libs.Games.GameTypes;

namespace WebGames.Controllers
{
    [Authorize(Roles = "player")]
    public class GamesController : Controller
    {
        #region Views
        public ActionResult ActiveGame()
        {
            var activeGameInfo = GameManager.GetActiveGameInfo(User.Identity.GetUserId());

            return GetPage(activeGameInfo);
        }

        public ActionResult ActiveGameMap()
        {
            var activeGameInfo = GameManager.GetActiveGameInfo(User.Identity.GetUserId());

            return GetPage(activeGameInfo, "Map");
        }

        public ActionResult ActiveExplainer()
        {
            var activeGameInfo = GameManager.GetActiveGameInfo(User.Identity.GetUserId());

            return GetPage(activeGameInfo, "Explainer");
        }

        public ActionResult ActiveGameDemo()
        {
            return View();
        }

        public ActionResult ActiveGameAfter()
        {
            var status = Request.QueryString["status"] ?? "";

            var activeGameInfo = GameManager.GetActiveGameInfo(User.Identity.GetUserId());

            if (activeGameInfo.ActiveGameDataModel != null)
            {
                var message = activeGameInfo.ActiveGameDataModel.Messages.ContainsKey(status) ? activeGameInfo.ActiveGameDataModel.Messages[status] : "";
                var page = $"{activeGameInfo.ActiveGameDataModel.ActiveGameKey}/ActiveGameAfter";
                return View(page, new Dictionary<string, string>() { { "message", message } });
            }
            else
            {
                return View("NoActiveGame");
            }
        }
        #endregion

        #region Game3

        public ActionResult Get_Random_Game3_Solution()
        {
            var Rnd = new Random(DateTime.UtcNow.Second);

            var res = new int[4];
            for (int i = 0; i < res.Length; i++)
            {
                var num = 0;
                do
                {
                    num = Rnd.Next(1, 7);
                } while (!res.Contains(num));
                res[i] = num;
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Game5

        public ActionResult CheckQuestion(int questionId, int answerIndex)
        {
            var UserId = User.Identity.GetUserId();
            // Security - Check if Game is the currently active one - cannot set the score for a non active game
            var CurrentActiveGameKey = GameManager.GetActiveGameInfo(UserId).ActiveGameDataModel.ActiveGameKey;
            if (CurrentActiveGameKey != GameKeys.GAME_5)
            {
                return Json(new { success = false, message = "Game is not active" }, JsonRequestBehavior.AllowGet);
            }

            var IsCorrect = Game5_Manager.CheckAndSaveQuestionAnswer(UserId, questionId, answerIndex); // Cannot override the score - once is set the done

            return Json(new { success = true, isCorrect = IsCorrect }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPlayerQuestions()
        {
            var res = new List<GameQuestionView>();
            try
            {
                res.AddRange(Game5_Manager.GetPlayerQuestions(User.Identity.GetUserId()));
            }
            catch (Exception exc)
            {
                Logger.Log(exc);
            }
            return Json(res);
        }

        #endregion

        public ActionResult Save_Game_Score(int score)
        {
            var UserId = User.Identity.GetUserId();

            // Security - Check if Game is the currently active one - cannot set the score for a non active game
            var ActiveGameKey = GameManager.GetActiveGameInfo(UserId).ActiveGameDataModel.ActiveGameKey;
            if (ActiveGameKey == "")
            {
                return Json(new { success = false, message = "No Game is Active" }, JsonRequestBehavior.AllowGet);
            }

            GameManager.GameDict[ActiveGameKey].SM.SetUserScore(UserId, score, EnableOverride: false); // Cannot override the score - once is set the done

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveGameTime(int remainingTime, long timestamp)
        {
            var UserId = User.Identity.GetUserId();
            ActivityManager.SyncPlayedTimeForToday(UserId, remainingTime, timestamp);
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetGameTime()
        {
            var UserId = User.Identity.GetUserId();
            var res = UserGameManager.GetUserRemainingTime(User.Identity.GetUserId());
            return Json(new { success = true, time = res }, JsonRequestBehavior.AllowGet);
        }

        // Helpers //
        private ActionResult GetPage(ActiveUserGameInfo gameInfo, string Page = "")
        {
            ActionResult ViewRes = null;
            // Active game was found
            if (gameInfo.ActiveGameDataModel != null)
            {
                // No remaining time
                if (gameInfo.RemainingTime <= 0)
                {
                    ViewRes = RedirectToAction("ActiveGameAfter", new { status = "outoftime" });
                } // no available levels
                else if (gameInfo.AvailableLevels > 0 && gameInfo.AvailableLevels < gameInfo.ActiveLevel)
                {
                    ViewRes = RedirectToAction("ActiveGameAfter", new { status = "outoftime" });
                }
                else
                {
                    string ViewPageToDisplay = gameInfo.PageFolder;
                    if (Page != "")
                    {
                        ViewPageToDisplay += $"/{Page}";
                    }
                    else // it's the game so use the PageFolder for the file
                    {
                        ViewPageToDisplay += $"/{gameInfo.PageFolder}";

                        // If level is on its own page
                        if (gameInfo.LevelAsPage)
                        {
                            // use level specific page
                            ViewPageToDisplay += $"-{gameInfo.ActiveLevel}";
                        }
                    }

                    ViewRes = View(ViewPageToDisplay, gameInfo);
                }
            }
            else
            {
                // Nothing was found
                ViewRes = View("NoActiveGame");
            }

            return ViewRes;
        }
    }
}