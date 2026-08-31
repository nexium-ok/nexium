exports.up = async (knex) => {
  await knex.schema.createTable('promocodes', (t) => {
    t.increments('id').primary();
    t.string('code', 50).notNullable().unique();
    t.bigInteger('asset_id').nullable();
    t.integer('robux').nullable();
    t.dateTime('created_at').notNullable().defaultTo(knex.fn.now());
    t.dateTime('expires_at').nullable();
    t.integer('maxuses').nullable();
    t.integer('uses').notNullable().defaultTo(0);
    t.boolean('active').notNullable().defaultTo(true);
  });

  await knex.schema.createTable('promocode_redemptions', (t) => {
    t.increments('id').primary();
    t.integer('promocode').notNullable().references('id').inTable('promocodes');
    t.bigInteger('user_id').notNullable();
    t.dateTime('redeemed_at').notNullable().defaultTo(knex.fn.now());
    t.bigInteger('asset_id').nullable();
    t.integer('robux').nullable();
  });
};

exports.down = async (knex) => {
  await knex.schema.dropTable('promocode_redemptions');
  await knex.schema.dropTable('promocodes');
};