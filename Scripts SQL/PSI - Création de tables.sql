CREATE DATABASE IF NOT EXISTS metro;

USE metro;

CREATE TABLE stations (
	id INT PRIMARY KEY,
	nom VARCHAR(100),
	ligne VARCHAR(10),
	latitude DOUBLE,
	longitude DOUBLE
);


CREATE TABLE liaisons (
	id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT,
    precedent INT,
    suivant INT,
    temps INT,
    changement INT,
    FOREIGN KEY (station_id) REFERENCES stations(id) ON DELETE CASCADE
);


CREATE TABLE correspondances (
    id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT,
    ligne_origine VARCHAR(10),
    ligne_correspondance VARCHAR(10),
    temps_correspondance INT,
    FOREIGN KEY (station_id) REFERENCES stations(id) ON DELETE CASCADE
);

