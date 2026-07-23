using System;
using LastTrain.Audio;
using LastTrain.Run;

namespace LastTrain.Passenger
{
    /// <summary>합성 판정·실행 순수 로직. UI와 분리되어 EditMode 테스트 가능.</summary>
    public static class MergeService
    {
        /// <summary>합성 성공 시 (resultingStar, passengerId).</summary>
        public static event Action<int, string> Merged;
        /// <summary>
        /// 같은 Passenger ID, 같은 Star Level, 최대 등급 미만일 때만 합성 가능.
        /// </summary>
        public static bool CanMerge(PassengerRuntime source, PassengerRuntime target)
        {
            if (source == null || target == null || ReferenceEquals(source, target))
            {
                return false;
            }

            if (source.Data == null || target.Data == null)
            {
                return false;
            }

            if (source.Data.Id != target.Data.Id)
            {
                return false;
            }

            if (source.StarLevel != target.StarLevel)
            {
                return false;
            }

            return source.StarLevel < source.Data.MaxStarLevel
                   && target.StarLevel < target.Data.MaxStarLevel;
        }

        /// <summary>
        /// source 슬롯 승객을 제거하고 target 슬롯 승객의 등급을 1 올린다.
        /// 합성 결과는 target 슬롯에 남는다.
        /// </summary>
        public static bool TryMerge(
            RunState runState,
            int sourceSlot,
            int targetSlot,
            out MergeResult result)
        {
            result = default;

            if (runState == null)
            {
                return false;
            }

            if (sourceSlot < 0 || sourceSlot >= RunState.GridSlotCount
                || targetSlot < 0 || targetSlot >= RunState.GridSlotCount
                || sourceSlot == targetSlot)
            {
                return false;
            }

            PassengerRuntime source = runState.GetPassengerAtSlot(sourceSlot);
            PassengerRuntime target = runState.GetPassengerAtSlot(targetSlot);

            if (!CanMerge(source, target))
            {
                return false;
            }

            if (!runState.TryConsumePassenger(sourceSlot, out PassengerRuntime consumed)
                || !ReferenceEquals(consumed, source))
            {
                return false;
            }

            if (!target.TryUpgradeStar())
            {
                // 이론상 CanMerge 통과 후 실패하지 않아야 함. 실패 시 원상 복구.
                runState.TryPlacePassenger(sourceSlot, source);
                return false;
            }

            // 합성 후 공격 쿨타임은 TryUpgradeStar에서 초기화된다.
            runState.RecordMerge(target.StarLevel, target.Data.Id);
            runState.TryPlacePendingPassengers();
            GameAudio.PlaySfx(SfxId.Merge);
            Merged?.Invoke(target.StarLevel, target.Data.Id);

            result = new MergeResult(
                sourceSlot,
                targetSlot,
                source.InstanceId,
                target.InstanceId,
                target.Data.Id,
                target.StarLevel);

            return true;
        }
    }

    /// <summary>합성 완료 결과. UI 연출용.</summary>
    public readonly struct MergeResult
    {
        public MergeResult(
            int sourceSlot,
            int targetSlot,
            string consumedInstanceId,
            string resultInstanceId,
            string passengerId,
            int resultingStarLevel)
        {
            SourceSlot = sourceSlot;
            TargetSlot = targetSlot;
            ConsumedInstanceId = consumedInstanceId;
            ResultInstanceId = resultInstanceId;
            PassengerId = passengerId;
            ResultingStarLevel = resultingStarLevel;
        }

        public int SourceSlot { get; }
        public int TargetSlot { get; }
        public string ConsumedInstanceId { get; }
        public string ResultInstanceId { get; }
        public string PassengerId { get; }
        public int ResultingStarLevel { get; }
    }
}
