# herramientaGonzalez2021
Herramienta que acompaña a la tesis Generalización del Modelado de Cadencias de Tecleo con Contextos Finitos para su Utilización en Ataques de Presentación y Canal Lateral


# Prerequsitos y dependencias
El código fuente requiere ser compilado bajo .NET Core 3.1, que se encuentra disponible para múltiples plataformas. El entorno alternativo de desarrollo Mono también puede ser utilizado para la tarea.

La compilación binaria requiere un sistema operativo Windows 7 o superior y .NET Framework 4.7.2 o .NET Core 3.1 previamente instalado.

La ejecución de algunos de los experimentos requiere que la herramienta de aprendizaje automático WEKA 3.8.1 o superior se encuentre previamente instalada.

# Ejecución por línea de comandos
La herramienta de análisis de cadencias de tecleo es una aplicación de consola desarrollada en C\# bajo .NET Core 3.1. La distribución binaria que se pone a disponibilidad en forma libre y gratuita ejecuta bajo el sistema operativo Windows, pero es factible compilar el código fuente utilizando versiones de .NET Core para otras plataformas como Linux o MacOS. Los parámetros de línea de comandos no se modifican en estos casos.

El patrón general de invocación tiene la forma

KSDExperiments.exe {EXPERIMENTO} [parámetros]

en donde EXPERIMENTO puede ser SYNTHESIZE, VERIFY, o IDENTIFY. Cuando se omiten los parámetros, la herramienta muestra los parámetros esperados para cada experimento.
