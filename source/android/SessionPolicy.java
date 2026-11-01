package app.xiaomidesk;
final class SessionPolicy {
 static boolean allows(boolean enabled,String currentToken,int currentPort,String queuedToken,int queuedPort){return enabled&&currentToken!=null&&!currentToken.isEmpty()&&currentToken.equals(queuedToken)&&currentPort>0&&currentPort==queuedPort;}
}
