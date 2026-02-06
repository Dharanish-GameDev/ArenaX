using Firebase.Auth;

public static class FirebaseExtensions
{
    public static FirebaseUser GetUser(this AuthResult authResult)
    {
        return authResult?.User;
    }
}