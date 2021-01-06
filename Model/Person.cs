namespace MoviesDotNetCore.Model
{
    public class Person
    {

        public string Name { get; }

        public string Job { get; }

        public string Role { get; }

        public Person(string name, string job, string role)
        {
            Name = name;
            Job = job;
            Role = role;
        }
    }
}