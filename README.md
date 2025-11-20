See the Azure-hosted version (removed link due to AI bot traffic constatntly eating the data allowance)

Lots of fun data explorations and visualisations for films, actors, genres etc.

A simple ASP.NET web app (MVC) using SQL server for a film database. 

Database definition is as follows:

film (film_id*, title, year, run_time_minutes)
person(person_id*, name, birth_year, death_year)
genre(genre_id*, name)
job(job_id*, title)
character(character_id*, name)
film_genre(film_genre_id*, film_id, genre_id)
film_person(film_id*, person_id*, job_id*)
film_person_character(film_id*, person_id*, character_id*)

Data taken from IMDB non-commercial datasets and cleaned-up:

https://developer.imdb.com/non-commercial-datasets/
