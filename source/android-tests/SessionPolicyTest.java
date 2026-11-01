package app.xiaomidesk;
public class SessionPolicyTest {
 static void check(boolean expected,boolean actual){if(expected!=actual)throw new AssertionError("session authorization mismatch");}
 public static void main(String[] args){
  check(true,SessionPolicy.allows(true,"token",123,"token",123));
  check(false,SessionPolicy.allows(false,"token",123,"token",123));
  check(false,SessionPolicy.allows(true,"new",123,"old",123));
  check(false,SessionPolicy.allows(true,"token",124,"token",123));
  check(false,SessionPolicy.allows(true,"",0,"token",123));
  System.out.println("PASS queued jobs rejected after pause, forget, or session replacement");
 }
}
