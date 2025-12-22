public static class FormConstants
{
    public static class SubmissionStatus
    {
        public const string UnderReview = "قيد المراجعة";
        public const string Approved = "مقبول";
        public const string Rejected = "مرفوض";
        public const string Deleted = "محذوف";
    }

    public static class FieldTypes
    {
        public const string Text = "text";
        public const string Number = "number";
        public const string Dropdown = "dropdown";
        public const string Date = "date";
    }

    public static class MandatoryFields
    {
        public static readonly HashSet<string> Names = new()
        {
            "fullName", "phoneNumber", "birthDate", "citizenshipStatus",
            "hasMortgage", "monthlySalary", "monthlyCommitments"
        };
    }
}