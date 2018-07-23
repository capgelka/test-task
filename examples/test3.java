public class Main {

    public static void method(boolean... conditions) {

        int x;

        x = 1;

        if (conditions[0]) {

            x = 2;

            if (conditions[1]) {

                x = 3;

            }

            x = 4;

            if (conditions[2]) {

                x = 19;

            }

            if (conditions[2]) {
                
                x = 8;

                if (conditions[9]) {
                    x = 7;
                }
            }


        }

        if (conditions[0]) {

            x = 6;

        }

        System.out.println(x);

    }

}