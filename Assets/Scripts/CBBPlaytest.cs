using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class CBBPlaytest
{
    private enum SimMove { Charge, Beam, Barrier }

    public static string LogPath
    {
        get
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.persistentDataPath;
            return Path.Combine(projectRoot, "PlaytestLogs", "CBB_playtest_log.csv");
        }
    }

    public static void LogMatch(
        string mode, int matchNumber, string winner, float durationSeconds,
        int rounds, int turns, int playerScore, int cpuScore,
        int playerChargeMoves, int playerBeamMoves, int playerBarrierMoves,
        int cpuChargeMoves, int cpuBeamMoves, int cpuBarrierMoves,
        int beamWins, int chargeWins, int suddenDeaths, int suddenDeathTurns)
    {
        string directory = Path.GetDirectoryName(LogPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        bool writeHeader = !File.Exists(LogPath);

        using (StreamWriter writer = new StreamWriter(LogPath, true, new UTF8Encoding(true)))
        {
            if (writeHeader)
            {
                writer.WriteLine("実行日時,モード,試合番号,勝者,試合時間秒,ラウンド数,総ターン数,プレイヤー得点,CPU得点,プレイヤー_チャージ,プレイヤー_ビーム,プレイヤー_バリア,CPU_チャージ,CPU_ビーム,CPU_バリア,ビーム勝利数,5チャージ勝利数,サドンデス回数,サドンデスターン数");
            }

            writer.WriteLine(string.Join(",", new string[]
            {
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), mode, matchNumber.ToString(), winner,
                durationSeconds.ToString("0.00"), rounds.ToString(), turns.ToString(),
                playerScore.ToString(), cpuScore.ToString(),
                playerChargeMoves.ToString(), playerBeamMoves.ToString(), playerBarrierMoves.ToString(),
                cpuChargeMoves.ToString(), cpuBeamMoves.ToString(), cpuBarrierMoves.ToString(),
                beamWins.ToString(), chargeWins.ToString(), suddenDeaths.ToString(), suddenDeathTurns.ToString()
            }));
        }
    }

    public static string RunAutomatedMatches(int matchCount)
    {
        int chatGptWins = 0;
        int cpuWins = 0;
        int totalTurnsAcrossMatches = 0;
        int totalSuddenDeaths = 0;

        for (int matchIndex = 1; matchIndex <= matchCount; matchIndex++)
        {
            int playerCharge = 0;
            int cpuCharge = 0;
            int playerScore = 0;
            int cpuScore = 0;
            int rounds = 0;
            int turns = 0;
            int playerChargeMoves = 0;
            int playerBeamMoves = 0;
            int playerBarrierMoves = 0;
            int cpuChargeMoves = 0;
            int cpuBeamMoves = 0;
            int cpuBarrierMoves = 0;
            int beamWins = 0;
            int chargeWins = 0;
            int suddenDeaths = 0;
            int suddenDeathTurns = 0;
            int[] playerHistory = new int[3];
            int[,] playerHistoryByCpuState = new int[2, 3];
            bool suddenDeath = false;
            float startTime = Time.realtimeSinceStartup;

            while (playerScore < 7 && cpuScore < 7 && turns < 1000)
            {
                SimMove playerMove = ChooseChatGptMove(playerCharge, cpuCharge, suddenDeath);
                int cpuStateBeforeTurn = cpuCharge > 0 ? 1 : 0;
                SimMove cpuMove = ChooseCpuMove(cpuCharge, playerCharge, suddenDeath, playerHistory, playerHistoryByCpuState);
                playerHistory[(int)playerMove]++;
                playerHistoryByCpuState[cpuStateBeforeTurn, (int)playerMove]++;
                turns++;
                if (suddenDeath) suddenDeathTurns++;
                CountMove(playerMove, ref playerChargeMoves, ref playerBeamMoves, ref playerBarrierMoves);
                CountMove(cpuMove, ref cpuChargeMoves, ref cpuBeamMoves, ref cpuBarrierMoves);

                if (playerMove == SimMove.Beam) playerCharge--;
                if (cpuMove == SimMove.Beam) cpuCharge--;

                bool roundEnded = false;
                if (playerMove == SimMove.Beam && cpuMove == SimMove.Charge)
                {
                    playerScore += suddenDeath ? playerCharge : 2;
                    beamWins++;
                    roundEnded = true;
                }
                else if (cpuMove == SimMove.Beam && playerMove == SimMove.Charge)
                {
                    cpuScore += suddenDeath ? cpuCharge : 2;
                    beamWins++;
                    roundEnded = true;
                }
                else
                {
                    if (playerMove == SimMove.Charge) playerCharge++;
                    if (cpuMove == SimMove.Charge) cpuCharge++;

                    if (!suddenDeath)
                    {
                        if (playerCharge >= 5 && cpuCharge >= 5)
                        {
                            suddenDeath = true;
                            suddenDeaths++;
                        }
                        else if (playerCharge >= 5)
                        {
                            playerScore += 5;
                            chargeWins++;
                            roundEnded = true;
                        }
                        else if (cpuCharge >= 5)
                        {
                            cpuScore += 5;
                            chargeWins++;
                            roundEnded = true;
                        }
                    }
                }

                if (roundEnded)
                {
                    rounds++;
                    playerCharge = 0;
                    cpuCharge = 0;
                    suddenDeath = false;
                }
            }

            string winner = playerScore >= 7 ? "ChatGPT" : "CPU";
            if (winner == "ChatGPT") chatGptWins++; else cpuWins++;
            totalTurnsAcrossMatches += turns;
            totalSuddenDeaths += suddenDeaths;

            LogMatch(
                "ChatGPT AI対CPU", matchIndex, winner, Time.realtimeSinceStartup - startTime,
                rounds, turns, playerScore, cpuScore,
                playerChargeMoves, playerBeamMoves, playerBarrierMoves,
                cpuChargeMoves, cpuBeamMoves, cpuBarrierMoves,
                beamWins, chargeWins, suddenDeaths, suddenDeathTurns);
        }

        float averageTurns = matchCount > 0 ? (float)totalTurnsAcrossMatches / matchCount : 0f;
        return $"自動テスト{matchCount}試合完了：ChatGPT {chatGptWins}勝 / CPU {cpuWins}勝\n平均 {averageTurns:0.0}ターン・サドンデス合計 {totalSuddenDeaths}回";
    }

    private static void CountMove(SimMove move, ref int charge, ref int beam, ref int barrier)
    {
        if (move == SimMove.Charge) charge++;
        else if (move == SimMove.Beam) beam++;
        else barrier++;
    }

    private static SimMove ChooseChatGptMove(int ownCharge, int enemyCharge, bool suddenDeath)
    {
        if (ownCharge <= 0) return UnityEngine.Random.value < 0.78f ? SimMove.Charge : SimMove.Barrier;

        float beamChance = 0.28f;
        float barrierChance = 0.22f;
        if (enemyCharge >= 4) beamChance += 0.25f;
        if (ownCharge >= 4 && !suddenDeath) beamChance += 0.08f;
        if (suddenDeath)
        {
            beamChance = ownCharge >= 8 ? 0.48f : 0.32f;
            barrierChance = enemyCharge >= 7 ? 0.3f : 0.2f;
        }

        float roll = UnityEngine.Random.value;
        if (roll < beamChance) return SimMove.Beam;
        if (roll < beamChance + barrierChance) return SimMove.Barrier;
        return SimMove.Charge;
    }

    private static SimMove ChooseCpuMove(int ownCharge, int enemyCharge, bool suddenDeath, int[] enemyHistory, int[,] enemyHistoryByCpuState)
    {
        if (ownCharge <= 0)
        {
            int emptyTotal = enemyHistoryByCpuState[0, 0] + enemyHistoryByCpuState[0, 1] + enemyHistoryByCpuState[0, 2];
            float predictedBeam = emptyTotal > 0
                ? (enemyHistoryByCpuState[0, (int)SimMove.Beam] + 1f) / (emptyTotal + 3f)
                : (enemyCharge > 0 ? 0.22f : 0f);
            float barrierChance = enemyCharge > 0 ? Mathf.Lerp(0.18f, 0.7f, predictedBeam) : 0.08f;
            return UnityEngine.Random.value < 1f - barrierChance ? SimMove.Charge : SimMove.Barrier;
        }

        int historyTotal = enemyHistory[0] + enemyHistory[1] + enemyHistory[2];
        int readyTotal = enemyHistoryByCpuState[1, 0] + enemyHistoryByCpuState[1, 1] + enemyHistoryByCpuState[1, 2];
        float chargeProbability;
        float beamProbability;
        float barrierProbability;

        if (readyTotal >= 2)
        {
            chargeProbability = (enemyHistoryByCpuState[1, (int)SimMove.Charge] + 1.2f) / (readyTotal + 3.6f);
            beamProbability = (enemyHistoryByCpuState[1, (int)SimMove.Beam] + 1.2f) / (readyTotal + 3.6f);
            barrierProbability = (enemyHistoryByCpuState[1, (int)SimMove.Barrier] + 1.2f) / (readyTotal + 3.6f);
        }
        else if (historyTotal >= 4)
        {
            chargeProbability = (enemyHistory[(int)SimMove.Charge] + 2f) / (historyTotal + 6f);
            beamProbability = (enemyHistory[(int)SimMove.Beam] + 2f) / (historyTotal + 6f);
            barrierProbability = (enemyHistory[(int)SimMove.Barrier] + 2f) / (historyTotal + 6f);
        }
        else
        {
            chargeProbability = enemyCharge <= 0 ? 0.48f : 0.38f;
            beamProbability = enemyCharge <= 0 ? 0f : 0.25f;
            barrierProbability = 1f - chargeProbability - beamProbability;
            if (enemyCharge >= 4)
            {
                chargeProbability = 0.32f;
                beamProbability = 0.43f;
                barrierProbability = 0.25f;
            }
        }

        float roundValue = suddenDeath ? Mathf.Max(1f, ownCharge - 1f) : 2f;
        float chargeReward = !suddenDeath && ownCharge == 4 ? 5f : 1.05f + ownCharge * 0.18f;
        float chargeUtility = (chargeProbability + barrierProbability) * chargeReward
            - beamProbability * (roundValue + 1.5f);
        float beamUtility = chargeProbability * (roundValue + 2.2f)
            - beamProbability * 0.55f - barrierProbability * 1.25f;
        float barrierUtility = beamProbability * 1.9f
            - chargeProbability * 0.65f - barrierProbability * 0.18f;

        if (barrierProbability > chargeProbability + 0.1f && barrierProbability > beamProbability)
            chargeUtility += ownCharge == 4 && !suddenDeath ? 2.8f : 1.5f;
        if (chargeProbability > barrierProbability && chargeProbability > beamProbability)
            beamUtility += 1.25f;
        if (beamProbability > chargeProbability && beamProbability > barrierProbability)
        {
            barrierUtility += 1.35f;
            chargeUtility += 0.35f;
        }
        if (enemyCharge <= 0)
        {
            chargeUtility += 0.9f;
            beamUtility -= 1.4f;
        }
        if (suddenDeath && ownCharge >= 8) beamUtility += 1f;
        if (enemyCharge >= 4 && !suddenDeath) beamUtility += 0.55f;

        float chargeWeight = Mathf.Exp(chargeUtility / 0.9f) * 0.9f + 0.1f;
        float beamWeight = Mathf.Exp(beamUtility / 0.9f) * 0.9f + 0.1f;
        float barrierWeight = Mathf.Exp(barrierUtility / 0.9f) * 0.9f + 0.1f;
        float roll = UnityEngine.Random.value * (chargeWeight + beamWeight + barrierWeight);
        if (roll < chargeWeight) return SimMove.Charge;
        if (roll < chargeWeight + beamWeight) return SimMove.Beam;
        return SimMove.Barrier;
    }
}
