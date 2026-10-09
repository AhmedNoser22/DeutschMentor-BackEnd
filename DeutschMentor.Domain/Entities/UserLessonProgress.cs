namespace DeutschMentor.Domain.Entities
{
    public class UserLessonProgress : BaseEntity
    {
        private const int AllStepsMask = 0b11111;

        public Guid UserId { get; set; }
        public Guid LessonId { get; set; }
        public int StepsSeenMask { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public bool AllStepsSeen => (StepsSeenMask & AllStepsMask) == AllStepsMask;

        public IReadOnlyList<LessonStep> SeenSteps =>
            Enum.GetValues<LessonStep>().Where(HasSeen).ToList();

        public bool HasSeen(LessonStep step) => (StepsSeenMask & Bit(step)) != 0;

        public void MarkSeen(LessonStep step) => StepsSeenMask |= Bit(step);

        private static int Bit(LessonStep step) => 1 << ((int)step - 1);
    }
}
