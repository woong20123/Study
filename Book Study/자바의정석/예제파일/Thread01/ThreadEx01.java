public class ThreadEx01 implements Runnable {
    static boolean autoSave = false;

    public static void main(String[] args){
        Thread t = new Thread(new ThreadEx01());
        t.setDaemon(true);
        t.start();

        for(int i=1; i <= 10 ; i++){
            try{
                Thread.sleep(1000);
            }catch(InterruptedException e){}
            System.out.print(i);

            if(i == 5)
                autoSave = true;
        }

        System.out.println("프로그램의 종료합니다.");
    }

    public void run() {
        while(true){
            try{
                Thread.sleep(3 * 1000);
            }catch(InterruptedException e){}

            if(autoSave){
                autoSave();
            }
        }
    }

    public void autoSave(){
        System.out.println("작업 파일이 자동 저장되었습니다.");
    }
}
