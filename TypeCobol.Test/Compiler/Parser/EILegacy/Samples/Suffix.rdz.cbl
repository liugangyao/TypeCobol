﻿000000*Don't except any errors 
000000 IDENTIFICATION DIVISION.
000000 PROGRAM-ID. MYPGM.
001930*REMARKS.         COPY=(                                 
002630*                       YPRTD20L YCONVEAL  
002700*                                 ).                     
000000 ENVIRONMENT DIVISION.
000000 CONFIGURATION SECTION.
000000 SOURCE-COMPUTER. IBM-370.
000000 DATA DIVISION.
000000 WORKING-STORAGE section.
000000    01 PRTD20L. COPY  YPRTD20L.   
000000    01 CONVEAL. COPY  YCONVEAL.   
000000    
000000
000000 PROCEDURE DIVISION.
000000     GOBACK
000000     .
000000 END PROGRAM MYPGM.